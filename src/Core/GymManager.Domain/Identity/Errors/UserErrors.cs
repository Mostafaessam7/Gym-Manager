using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Identity.Errors;

/// <summary>Catalog of expected, domain-relevant failures for the <see cref="User"/> aggregate.</summary>
public static class UserErrors
{
    public static Error EmailAlreadyInUse(string email) =>
        Error.Conflict("User.EmailAlreadyInUse", $"The email '{email}' is already registered.");

    public static readonly Error NotFound = Error.NotFound("User.NotFound", "The user was not found.");

    public static readonly Error InvalidCredentials = Error.Unauthorized("User.InvalidCredentials", "The email or password is incorrect.");

    public static readonly Error AccountDeactivated = Error.Forbidden("User.AccountDeactivated", "This account has been deactivated.");

    public static readonly Error RefreshTokenInvalid = Error.Unauthorized("User.RefreshTokenInvalid", "The refresh token is invalid or has expired.");

    public static Error AccountLockedOut(DateTimeOffset lockedOutUntilUtc) => Error.Forbidden(
        "User.AccountLockedOut",
        $"This account is temporarily locked due to repeated failed sign-in attempts. Try again after {lockedOutUntilUtc:O}.");

    public static readonly Error PasswordResetTokenInvalid =
        Error.Unauthorized("User.PasswordResetTokenInvalid", "The password reset link is invalid or has expired.");

    public static readonly Error AlreadyInRole = Error.Conflict("User.AlreadyInRole", "The user already has this role.");

    public static readonly Error NotInRole = Error.NotFound("User.NotInRole", "The user does not have this role.");

    public static readonly Error EmailVerificationTokenInvalid = Error.Unauthorized(
        "User.EmailVerificationTokenInvalid", "The email verification link is invalid or has expired.");

    public static readonly Error EmailAlreadyVerified =
        Error.Conflict("User.EmailAlreadyVerified", "This email address has already been verified.");

    public static readonly Error PasswordReusesRecentPassword = Error.Validation(
        "User.PasswordReusesRecentPassword", "You cannot reuse one of your last 5 passwords.");

    public static readonly Error TwoFactorAlreadyEnabled =
        Error.Conflict("User.TwoFactorAlreadyEnabled", "Two-factor authentication is already enabled.");

    public static readonly Error TwoFactorNotEnabled =
        Error.Conflict("User.TwoFactorNotEnabled", "Two-factor authentication is not enabled.");

    public static readonly Error TwoFactorCodeInvalid =
        Error.Unauthorized("User.TwoFactorCodeInvalid", "The two-factor authentication code is invalid.");

    public static readonly Error TwoFactorChallengeRequired = Error.Unauthorized(
        "User.TwoFactorChallengeRequired", "A two-factor authentication code is required to complete sign-in.");
}
