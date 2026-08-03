using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Invoices.Errors;

public static class InvoiceErrors
{
    public static readonly Error NotFound = Error.NotFound("Invoice.NotFound", "The invoice was not found.");

    public static readonly Error NoLines = Error.Validation("Invoice.NoLines", "An invoice must have at least one line.");

    public static readonly Error NotDraft = Error.Conflict("Invoice.NotDraft", "Only a draft invoice can be modified or issued.");

    public static readonly Error NotIssued = Error.Conflict("Invoice.NotIssued", "Only an issued invoice can be marked as paid.");

    public static readonly Error AlreadyPaid = Error.Conflict("Invoice.AlreadyPaid", "A paid invoice cannot be voided.");
}
