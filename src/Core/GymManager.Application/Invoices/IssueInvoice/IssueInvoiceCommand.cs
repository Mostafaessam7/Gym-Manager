using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Invoices.IssueInvoice;

public sealed record IssueInvoiceCommand(Guid InvoiceId) : ICommand;
