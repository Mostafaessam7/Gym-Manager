using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Invoices.VoidInvoice;

public sealed record VoidInvoiceCommand(Guid InvoiceId) : ICommand;
