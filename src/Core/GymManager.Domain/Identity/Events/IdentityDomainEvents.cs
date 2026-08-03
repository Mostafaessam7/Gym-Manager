using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Identity.Events;

public sealed record UserRegisteredDomainEvent(Guid UserId, string Email) : IDomainEvent;

public sealed record UserLoggedInDomainEvent(Guid UserId) : IDomainEvent;

public sealed record UserDeactivatedDomainEvent(Guid UserId) : IDomainEvent;

public sealed record RoleAssignedToUserDomainEvent(Guid UserId, Guid RoleId) : IDomainEvent;

public sealed record RoleRevokedFromUserDomainEvent(Guid UserId, Guid RoleId) : IDomainEvent;
