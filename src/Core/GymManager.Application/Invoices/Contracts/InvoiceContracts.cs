namespace GymManager.Application.Invoices.Contracts;

public sealed record InvoiceLineRequest(string Description, int Quantity, decimal UnitPrice);

public sealed record InvoiceLineResponse(string Description, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record InvoiceResponse(
    Guid Id,
    string InvoiceNumber,
    Guid MemberId,
    Guid BranchId,
    DateTimeOffset IssuedOnUtc,
    DateTimeOffset DueOnUtc,
    string Status,
    Guid? PaymentId,
    decimal TotalAmount,
    string Currency,
    IReadOnlyCollection<InvoiceLineResponse> Lines);
