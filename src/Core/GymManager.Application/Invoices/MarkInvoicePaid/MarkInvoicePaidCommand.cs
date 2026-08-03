using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Invoices.MarkInvoicePaid;

public sealed record MarkInvoicePaidCommand(Guid InvoiceId, Guid PaymentId) : ICommand;
