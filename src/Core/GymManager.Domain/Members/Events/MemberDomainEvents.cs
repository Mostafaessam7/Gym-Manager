using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Members.Events;

public sealed record MemberRegisteredDomainEvent(Guid MemberId, Guid BranchId) : IDomainEvent;

public sealed record MemberStatusChangedDomainEvent(Guid MemberId, MemberStatus PreviousStatus, MemberStatus NewStatus) : IDomainEvent;
