using GymManager.Domain.Sales;

namespace GymManager.Application.Sales.Contracts;

public static class SaleMappingExtensions
{
    public static SaleResponse ToResponse(this Sale sale) => new(
        sale.Id, sale.BranchId, sale.MemberId, sale.SoldByUserId, sale.Status.ToString(), sale.PaymentId, sale.SoldOnUtc,
        sale.TotalAmount.Amount, sale.Currency,
        sale.Lines.Select(l => new SaleLineResponse(
            l.Id, l.ProductId, l.ProductNameSnapshot, l.Quantity, l.UnitPrice.Amount, l.LineTotal.Amount, l.RefundedQuantity, l.RemainingQuantity)).ToArray(),
        sale.Payments.Select(p => new SalePaymentResponse(p.Id, p.Method.ToString(), p.Amount.Amount, p.PaymentId, p.GiftCardId)).ToArray());
}
