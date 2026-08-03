using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Classes.Events;

public sealed record ClassSessionScheduledDomainEvent(Guid ClassSessionId, Guid GymClassId, Guid TrainerId) : IDomainEvent;

public sealed record ClassSessionBookedDomainEvent(Guid ClassSessionId, Guid MemberId) : IDomainEvent;

public sealed record ClassBookingCancelledDomainEvent(Guid ClassSessionId, Guid MemberId) : IDomainEvent;

public sealed record ClassSessionCancelledDomainEvent(Guid ClassSessionId) : IDomainEvent;
