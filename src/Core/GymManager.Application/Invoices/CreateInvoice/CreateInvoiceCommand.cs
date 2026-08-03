using GymManager.Application.Invoices.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Invoices.CreateInvoice;

public sealed record CreateInvoiceCommand(
    Guid MemberId, Guid BranchId, DateTimeOffset DueOnUtc, string Currency, IReadOnlyCollection<InvoiceLineRequest> Lines)
    : ICommand<Result<InvoiceResponse>>;
