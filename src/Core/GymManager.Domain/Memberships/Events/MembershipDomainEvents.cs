using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Memberships.Events;

public sealed record MembershipPurchasedDomainEvent(Guid MembershipId, Guid MemberId, Guid MembershipPlanId) : IDomainEvent;

public sealed record MembershipRenewedDomainEvent(Guid MembershipId, Guid MemberId, DateOnly NewEndDate) : IDomainEvent;

public sealed record MembershipExpiredDomainEvent(Guid MembershipId, Guid MemberId) : IDomainEvent;

public sealed record MembershipCancelledDomainEvent(Guid MembershipId, Guid MemberId) : IDomainEvent;
