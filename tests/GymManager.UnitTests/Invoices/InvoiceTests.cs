using GymManager.Domain.Common;
using GymManager.Domain.Invoices;
using Xunit;

namespace GymManager.UnitTests.Invoices;

public sealed class InvoiceTests
{
    private static Invoice CreateDraftInvoice() =>
        Invoice.CreateDraft("INV-000001", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(14));

    [Fact]
    public void Issue_Should_Fail_When_No_Lines()
    {
        var invoice = CreateDraftInvoice();

        var result = invoice.Issue();

        Assert.True(result.IsFailure);
        Assert.Equal("Invoice.NoLines", result.Error.Code);
    }

    [Fact]
    public void TotalAmount_Should_Sum_All_Line_Totals()
    {
        var invoice = CreateDraftInvoice();
        invoice.AddLine("Monthly membership", 1, Money.Create(50m).Value);
        invoice.AddLine("Personal training session", 2, Money.Create(30m).Value);

        Assert.Equal(110m, invoice.TotalAmount.Amount);
    }

    [Fact]
    public void Issue_Should_Succeed_When_Lines_Exist()
    {
        var invoice = CreateDraftInvoice();
        invoice.AddLine("Monthly membership", 1, Money.Create(50m).Value);

        var result = invoice.Issue();

        Assert.True(result.IsSuccess);
        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
    }

    [Fact]
    public void AddLine_Should_Fail_Once_Invoice_Is_No_Longer_Draft()
    {
        var invoice = CreateDraftInvoice();
        invoice.AddLine("Monthly membership", 1, Money.Create(50m).Value);
        invoice.Issue();

        var result = invoice.AddLine("Late fee", 1, Money.Create(10m).Value);

        Assert.True(result.IsFailure);
        Assert.Equal("Invoice.NotDraft", result.Error.Code);
    }

    [Fact]
    public void MarkPaid_Should_Fail_When_Not_Issued()
    {
        var invoice = CreateDraftInvoice();

        var result = invoice.MarkPaid(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Invoice.NotIssued", result.Error.Code);
    }

    [Fact]
    public void Void_Should_Fail_When_Already_Paid()
    {
        var invoice = CreateDraftInvoice();
        invoice.AddLine("Monthly membership", 1, Money.Create(50m).Value);
        invoice.Issue();
        invoice.MarkPaid(Guid.NewGuid());

        var result = invoice.Void();

        Assert.True(result.IsFailure);
        Assert.Equal("Invoice.AlreadyPaid", result.Error.Code);
    }
}
