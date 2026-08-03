using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Identity.Sessions.RevokeAllSessions;

/// <summary>"Log out everywhere" — revokes every active session for the user, including the one making this
/// request (the access token used to call this endpoint remains valid until it expires on its own, since
/// access tokens are never persisted or checked against revocation state).</summary>
public sealed record RevokeAllSessionsCommand(Guid UserId) : ICommand;
