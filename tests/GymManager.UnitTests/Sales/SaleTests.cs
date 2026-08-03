using GymManager.Domain.Common;
using GymManager.Domain.Payments;
using GymManager.Domain.Sales;
using Xunit;

namespace GymManager.UnitTests.Sales;

public sealed class SaleTests
{
    [Fact]
    public void Create_Should_Fail_When_No_Lines()
    {
        var result = Sale.Create(Guid.NewGuid(), null, Guid.NewGuid(), []);

        Assert.True(result.IsFailure);
        Assert.Equal("Sale.NoLines", result.Error.Code);
    }

    [Fact]
    public void Create_Should_Compute_TotalAmount_From_Lines()
    {
        var lines = new List<(Guid ProductId, string ProductName, int Quantity, Money UnitPrice)>
        {
            (Guid.NewGuid(), "Protein Shake", 2, Money.Create(5m).Value),
            (Guid.NewGuid(), "Energy Bar", 3, Money.Create(2m).Value),
        };

        var result = Sale.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), lines);

        Assert.True(result.IsSuccess);
        Assert.Equal(16m, result.Value.TotalAmount.Amount);
        Assert.Single(result.Value.DomainEvents);
    }

    [Fact]
    public void Refund_Should_Fail_When_Already_Refunded()
    {
        var lines = new List<(Guid ProductId, string ProductName, int Quantity, Money UnitPrice)>
        {
            (Guid.NewGuid(), "Protein Shake", 1, Money.Create(5m).Value),
        };
        var sale = Sale.Create(Guid.NewGuid(), null, Guid.NewGuid(), lines).Value;
        sale.Refund();

        var result = sale.Refund();

        Assert.True(result.IsFailure);
        Assert.Equal("Sale.AlreadyRefunded", result.Error.Code);
    }

    private static Sale CreateSaleWithOneLine(int quantity = 2)
    {
        var lines = new List<(Guid ProductId, string ProductName, int Quantity, Money UnitPrice)>
        {
            (Guid.NewGuid(), "Protein Shake", quantity, Money.Create(5m).Value),
        };
        return Sale.Create(Guid.NewGuid(), null, Guid.NewGuid(), lines).Value;
    }

    [Fact]
    public void AddPayment_Should_Set_PaymentId_To_The_First_Allocation()
    {
        var sale = CreateSaleWithOneLine();
        var firstPaymentId = Guid.NewGuid();
        var secondPaymentId = Guid.NewGuid();

        sale.AddPayment(PaymentMethod.Cash, Money.Create(5m).Value, firstPaymentId);
        sale.AddPayment(PaymentMethod.Card, Money.Create(5m).Value, secondPaymentId);

        Assert.Equal(firstPaymentId, sale.PaymentId);
        Assert.Equal(2, sale.Payments.Count);
    }

    [Fact]
    public void RefundLine_Should_Reduce_Remaining_Quantity_And_Mark_PartiallyRefunded()
    {
        var sale = CreateSaleWithOneLine(quantity: 2);
        var line = sale.Lines.Single();

        var result = sale.RefundLine(line.Id, 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(5m, result.Value.Amount);
        Assert.Equal(1, line.RemainingQuantity);
        Assert.Equal(SaleStatus.PartiallyRefunded, sale.Status);
    }

    [Fact]
    public void RefundLine_Refunding_The_Entire_Quantity_Should_Mark_The_Sale_Fully_Refunded()
    {
        var sale = CreateSaleWithOneLine(quantity: 2);
        var line = sale.Lines.Single();

        var result = sale.RefundLine(line.Id, 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(SaleStatus.Refunded, sale.Status);
    }

    [Fact]
    public void RefundLine_With_A_Quantity_Exceeding_Remaining_Should_Fail()
    {
        var sale = CreateSaleWithOneLine(quantity: 2);
        var line = sale.Lines.Single();

        var result = sale.RefundLine(line.Id, 3);

        Assert.True(result.IsFailure);
        Assert.Equal("Sale.RefundQuantityExceedsRemaining", result.Error.Code);
    }

    [Fact]
    public void RefundLine_With_An_Unknown_LineId_Should_Fail()
    {
        var sale = CreateSaleWithOneLine();

        var result = sale.RefundLine(Guid.NewGuid(), 1);

        Assert.True(result.IsFailure);
        Assert.Equal("Sale.LineNotFound", result.Error.Code);
    }

    [Fact]
    public void Refund_After_A_Partial_Refund_Should_Refund_Only_The_Remaining_Quantity()
    {
        var sale = CreateSaleWithOneLine(quantity: 3);
        var line = sale.Lines.Single();
        sale.RefundLine(line.Id, 1);

        var result = sale.Refund();

        Assert.True(result.IsSuccess);
        Assert.Equal(0, line.RemainingQuantity);
        Assert.Equal(SaleStatus.Refunded, sale.Status);
    }
}
