using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Payments.Events;

public sealed record PaymentCompletedDomainEvent(Guid PaymentId, Guid MemberId, decimal Amount, string Currency) : IDomainEvent;

public sealed record PaymentRefundedDomainEvent(Guid PaymentId, Guid MemberId) : IDomainEvent;

public sealed record PaymentFailedDomainEvent(Guid PaymentId, Guid MemberId) : IDomainEvent;
