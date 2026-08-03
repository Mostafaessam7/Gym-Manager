using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Invoices.Events;

public sealed record InvoiceIssuedDomainEvent(Guid InvoiceId, Guid MemberId) : IDomainEvent;

public sealed record InvoicePaidDomainEvent(Guid InvoiceId, Guid MemberId) : IDomainEvent;

public sealed record InvoiceVoidedDomainEvent(Guid InvoiceId) : IDomainEvent;
