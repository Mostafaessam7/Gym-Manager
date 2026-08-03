using GymManager.Domain.Invoices;

namespace GymManager.Application.Invoices.Contracts;

public static class InvoiceMappingExtensions
{
    public static InvoiceResponse ToResponse(this Invoice invoice) => new(
        invoice.Id, invoice.InvoiceNumber, invoice.MemberId, invoice.BranchId, invoice.IssuedOnUtc, invoice.DueOnUtc,
        invoice.Status.ToString(), invoice.PaymentId, invoice.TotalAmount.Amount, invoice.Currency,
        invoice.Lines.Select(l => new InvoiceLineResponse(l.Description, l.Quantity, l.UnitPrice.Amount, l.LineTotal.Amount)).ToArray());
}
