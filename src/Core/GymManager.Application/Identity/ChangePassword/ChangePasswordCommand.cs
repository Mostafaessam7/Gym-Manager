using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Identity.ChangePassword;

/// <summary>Changes the *current, authenticated* caller's own password, proving ownership with the current
/// password rather than a mailed reset token. <see cref="UserId"/> always comes from the caller's own JWT
/// (see the controller), never from the request body — nobody can change another user's password this way.</summary>
public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : ICommand;
