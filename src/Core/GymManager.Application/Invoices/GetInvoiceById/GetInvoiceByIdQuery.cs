using GymManager.Application.Invoices.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Invoices.GetInvoiceById;

public sealed record GetInvoiceByIdQuery(Guid InvoiceId) : IQuery<Result<InvoiceResponse>>;
