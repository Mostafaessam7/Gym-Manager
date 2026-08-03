using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Identity.Sessions.RevokeSession;

public sealed record RevokeSessionCommand(Guid UserId, Guid SessionId) : ICommand;
