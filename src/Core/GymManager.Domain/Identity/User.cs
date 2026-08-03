using GymManager.Domain.Common;
using GymManager.Domain.Identity.Errors;
using GymManager.Domain.Identity.Events;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Identity;

/// <summary>
/// A person who can authenticate against the system — staff, trainer, or admin. Members who never need
/// to sign in are modeled separately by the Members module and are not represented here.
/// </summary>
public sealed class User : AggregateRoot<Guid>, IAuditableEntity, ISoftDeletableEntity
{
    /// <summary>How many of a user's most recent passwords are checked against on a change/reset, to stop
    /// them cycling straight back to something they used a few changes ago.</summary>
    public const int PasswordHistoryLimit = 5;

    private readonly List<UserRole> _roles = [];
    private readonly List<RefreshToken> _refreshTokens = [];
    private readonly List<PasswordHistoryEntry> _passwordHistory = [];
    private readonly List<TwoFactorRecoveryCode> _twoFactorRecoveryCodes = [];

    private User()
    {
        Email = null!;
        PasswordHash = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
    }

    private User(Guid id, Email email, string passwordHash, string firstName, string lastName, Guid? branchId)
        : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        BranchId = branchId;
        IsActive = true;
    }

    public Email Email { get; private set; }

    public string PasswordHash { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string? PhoneNumber { get; private set; }

    public Guid? BranchId { get; private set; }

    public bool IsActive { get; private set; }

    public int FailedLoginAttempts { get; private set; }

    public DateTimeOffset? LockedOutUntilUtc { get; private set; }

    public string? PasswordResetTokenHash { get; private set; }

    public DateTimeOffset? PasswordResetTokenExpiresOnUtc { get; private set; }

    public bool IsEmailVerified { get; private set; }

    public string? EmailVerificationTokenHash { get; private set; }

    public DateTimeOffset? EmailVerificationTokenExpiresOnUtc { get; private set; }

    public bool TwoFactorEnabled { get; private set; }

    /// <summary>The TOTP shared secret (Base32). Set as soon as setup begins but only takes effect at login
    /// once <see cref="TwoFactorEnabled"/> is confirmed via <see cref="ConfirmTwoFactorSetup"/> — this
    /// two-step flow stops a setup a user abandons midway from silently locking them out later.</summary>
    public string? TwoFactorSecretKey { get; private set; }

    /// <summary>A short-lived, single-use token identifying a password-verified login that is now waiting on
    /// a second factor. Only its hash is stored, matching the password-reset-token pattern.</summary>
    public string? TwoFactorChallengeTokenHash { get; private set; }

    public DateTimeOffset? TwoFactorChallengeTokenExpiresOnUtc { get; private set; }

    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public IReadOnlyCollection<PasswordHistoryEntry> PasswordHistory => _passwordHistory.AsReadOnly();

    public IReadOnlyCollection<TwoFactorRecoveryCode> TwoFactorRecoveryCodes => _twoFactorRecoveryCodes.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedOnUtc { get; private set; }

    public string? DeletedBy { get; private set; }

    public static User Register(Email email, string passwordHash, string firstName, string lastName, Guid? branchId = null)
    {
        var user = new User(Guid.NewGuid(), email, passwordHash, firstName.Trim(), lastName.Trim(), branchId);
        user.Raise(new UserRegisteredDomainEvent(user.Id, email.Value));
        return user;
    }

    /// <summary>Consecutive failed attempts before an account is temporarily locked out.</summary>
    public const int MaxFailedLoginAttempts = 5;

    /// <summary>How long an account stays locked out after crossing <see cref="MaxFailedLoginAttempts"/>.</summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public Result VerifyIsActive() =>
        IsActive ? Result.Success() : Result.Failure(UserErrors.AccountDeactivated);

    public Result VerifyIsNotLockedOut(DateTimeOffset utcNow) =>
        LockedOutUntilUtc.HasValue && LockedOutUntilUtc.Value > utcNow
            ? Result.Failure(UserErrors.AccountLockedOut(LockedOutUntilUtc.Value))
            : Result.Success();

    public void RecordLogin()
    {
        FailedLoginAttempts = 0;
        LockedOutUntilUtc = null;
        Raise(new UserLoggedInDomainEvent(Id));
    }

    /// <summary>Records a failed sign-in attempt, locking the account out for <see cref="LockoutDuration"/>
    /// once <see cref="MaxFailedLoginAttempts"/> consecutive failures have accumulated. A prior lockout that
    /// has already expired is cleared before counting this attempt, so the counter reflects only the current
    /// run of failures.</summary>
    public void RecordFailedLoginAttempt(DateTimeOffset utcNow)
    {
        if (LockedOutUntilUtc.HasValue && LockedOutUntilUtc.Value <= utcNow)
        {
            FailedLoginAttempts = 0;
            LockedOutUntilUtc = null;
        }

        FailedLoginAttempts++;

        if (FailedLoginAttempts >= MaxFailedLoginAttempts)
            LockedOutUntilUtc = utcNow.Add(LockoutDuration);
    }

    public void Deactivate()
    {
        if (!IsActive) return;

        IsActive = false;
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
            token.Revoke();

        Raise(new UserDeactivatedDomainEvent(Id));
    }

    public void Reactivate() => IsActive = true;

    public void UpdateProfile(string firstName, string lastName, string? phoneNumber)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = phoneNumber?.Trim();
    }

    /// <summary>Changes the password, remembering the outgoing hash in <see cref="PasswordHistory"/> (capped
    /// at <see cref="PasswordHistoryLimit"/> entries, oldest dropped first). Callers are responsible for
    /// checking the new password isn't a reuse of a recent one first — that check needs
    /// <c>IPasswordHasher.Verify</c> against plaintext, which is an infrastructure concern the domain layer
    /// doesn't have access to.</summary>
    public void ChangePassword(string newPasswordHash, DateTimeOffset utcNow)
    {
        _passwordHistory.Add(new PasswordHistoryEntry(PasswordHash, utcNow));
        while (_passwordHistory.Count > PasswordHistoryLimit)
            _passwordHistory.RemoveAt(0);

        PasswordHash = newPasswordHash;
    }

    /// <summary>Stores the hash of a freshly issued password-reset token. The raw token is emailed to the
    /// user and never persisted — only its hash, so a database read alone can never yield a usable token.</summary>
    public void SetPasswordResetToken(string tokenHash, DateTimeOffset expiresOnUtc)
    {
        PasswordResetTokenHash = tokenHash;
        PasswordResetTokenExpiresOnUtc = expiresOnUtc;
    }

    /// <summary>Validates the presented token's hash against the stored one and, if valid and unexpired,
    /// applies the new password hash and invalidates the token (single use). Also clears any existing
    /// account lockout, since a successful reset proves ownership as strongly as a correct password would.</summary>
    public Result ResetPassword(string tokenHash, string newPasswordHash, DateTimeOffset utcNow)
    {
        if (PasswordResetTokenHash is null || PasswordResetTokenExpiresOnUtc is null)
            return Result.Failure(UserErrors.PasswordResetTokenInvalid);

        if (PasswordResetTokenExpiresOnUtc.Value <= utcNow)
            return Result.Failure(UserErrors.PasswordResetTokenInvalid);

        if (!string.Equals(PasswordResetTokenHash, tokenHash, StringComparison.Ordinal))
            return Result.Failure(UserErrors.PasswordResetTokenInvalid);

        ChangePassword(newPasswordHash, utcNow);
        PasswordResetTokenHash = null;
        PasswordResetTokenExpiresOnUtc = null;
        FailedLoginAttempts = 0;
        LockedOutUntilUtc = null;

        foreach (var token in _refreshTokens.Where(t => t.IsActive))
            token.Revoke();

        return Result.Success();
    }

    public Result AssignRole(Guid roleId)
    {
        if (_roles.Any(r => r.RoleId == roleId))
            return Result.Failure(UserErrors.AlreadyInRole);

        _roles.Add(new UserRole(roleId));
        Raise(new RoleAssignedToUserDomainEvent(Id, roleId));
        return Result.Success();
    }

    public Result RevokeRole(Guid roleId)
    {
        var userRole = _roles.FirstOrDefault(r => r.RoleId == roleId);
        if (userRole is null)
            return Result.Failure(UserErrors.NotInRole);

        _roles.Remove(userRole);
        Raise(new RoleRevokedFromUserDomainEvent(Id, roleId));
        return Result.Success();
    }

    /// <summary><paramref name="tokenHash"/> must already be hashed by the caller (Application layer, via
    /// <c>SecureTokenHasher</c>) — the Domain layer never sees or stores a raw refresh token.</summary>
    public RefreshToken IssueRefreshToken(string tokenHash, DateTimeOffset expiresOnUtc, string? ipAddress = null, string? userAgent = null)
    {
        var refreshToken = new RefreshToken(tokenHash, expiresOnUtc, ipAddress, userAgent);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    /// <summary><paramref name="currentTokenHash"/>/<paramref name="newTokenHash"/> must already be hashed by
    /// the caller — see <see cref="IssueRefreshToken"/>.</summary>
    public Result<RefreshToken> RotateRefreshToken(
        string currentTokenHash, string newTokenHash, DateTimeOffset newExpiresOnUtc, string? ipAddress = null, string? userAgent = null)
    {
        var existing = _refreshTokens.FirstOrDefault(t => t.TokenHash == currentTokenHash);
        if (existing is null || !existing.IsActive)
            return Result.Failure<RefreshToken>(UserErrors.RefreshTokenInvalid);

        existing.Revoke();
        var rotated = new RefreshToken(newTokenHash, newExpiresOnUtc, ipAddress, userAgent);
        _refreshTokens.Add(rotated);
        return Result.Success(rotated);
    }

    /// <summary>Revokes a single refresh token (sign-out of one session), leaving the user's other active
    /// sessions untouched. Idempotent: revoking an already-inactive or unknown token is not an error, so a
    /// client can always call logout safely regardless of the token's current state. <paramref name="tokenHash"/>
    /// must already be hashed by the caller — see <see cref="IssueRefreshToken"/>.</summary>
    public void RevokeRefreshToken(string tokenHash)
    {
        var existing = _refreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash && t.IsActive);
        existing?.Revoke();
    }

    /// <summary>Revokes one specific active session (identified by its <see cref="RefreshToken"/> id rather
    /// than the token value itself, so a "manage my devices" screen never needs to hold a live token).</summary>
    public Result RevokeSession(Guid sessionId)
    {
        var session = _refreshTokens.FirstOrDefault(t => t.Id == sessionId && t.IsActive);
        if (session is null)
            return Result.Failure(UserErrors.RefreshTokenInvalid);

        session.Revoke();
        return Result.Success();
    }

    /// <summary>Revokes every active session except (optionally) the one making this request — the "log out
    /// everywhere else" action.</summary>
    public void RevokeAllSessions(Guid? exceptSessionId = null)
    {
        foreach (var token in _refreshTokens.Where(t => t.IsActive && t.Id != exceptSessionId))
            token.Revoke();
    }

    /// <summary>Issues a fresh email-verification token, superseding any previous unverified one. Re-issuable
    /// any number of times (e.g. "resend verification email") until <see cref="VerifyEmail"/> succeeds.</summary>
    public void SetEmailVerificationToken(string tokenHash, DateTimeOffset expiresOnUtc)
    {
        EmailVerificationTokenHash = tokenHash;
        EmailVerificationTokenExpiresOnUtc = expiresOnUtc;
    }

    public Result VerifyEmail(string tokenHash, DateTimeOffset utcNow)
    {
        if (IsEmailVerified)
            return Result.Failure(UserErrors.EmailAlreadyVerified);

        if (EmailVerificationTokenHash is null || EmailVerificationTokenExpiresOnUtc is null)
            return Result.Failure(UserErrors.EmailVerificationTokenInvalid);

        if (EmailVerificationTokenExpiresOnUtc.Value <= utcNow)
            return Result.Failure(UserErrors.EmailVerificationTokenInvalid);

        if (!string.Equals(EmailVerificationTokenHash, tokenHash, StringComparison.Ordinal))
            return Result.Failure(UserErrors.EmailVerificationTokenInvalid);

        IsEmailVerified = true;
        EmailVerificationTokenHash = null;
        EmailVerificationTokenExpiresOnUtc = null;
        return Result.Success();
    }

    /// <summary>Marks the email verified without a token — used only for system-seeded accounts (e.g. the
    /// default Owner) that never go through the registration flow.</summary>
    public void MarkEmailVerified() => IsEmailVerified = true;

    /// <summary>Begins 2FA setup: stores the TOTP secret but does not yet enable 2FA at login. The caller
    /// (Infrastructure) generates the secret and the QR provisioning URI; the domain only tracks state.</summary>
    public Result StartTwoFactorSetup(string secretKey)
    {
        if (TwoFactorEnabled)
            return Result.Failure(UserErrors.TwoFactorAlreadyEnabled);

        TwoFactorSecretKey = secretKey;
        return Result.Success();
    }

    /// <summary>Activates 2FA once the user has proven they can generate a valid code from the pending
    /// secret, and issues one-time recovery codes (only their hashes are kept; the plaintext codes are
    /// returned to the caller exactly once and can never be retrieved again).</summary>
    public Result ConfirmTwoFactorSetup(IReadOnlyCollection<string> recoveryCodeHashes)
    {
        if (TwoFactorEnabled)
            return Result.Failure(UserErrors.TwoFactorAlreadyEnabled);

        if (TwoFactorSecretKey is null)
            return Result.Failure(UserErrors.TwoFactorNotEnabled);

        TwoFactorEnabled = true;
        _twoFactorRecoveryCodes.Clear();
        foreach (var hash in recoveryCodeHashes)
            _twoFactorRecoveryCodes.Add(new TwoFactorRecoveryCode(hash));

        return Result.Success();
    }

    public Result DisableTwoFactor()
    {
        if (!TwoFactorEnabled)
            return Result.Failure(UserErrors.TwoFactorNotEnabled);

        TwoFactorEnabled = false;
        TwoFactorSecretKey = null;
        _twoFactorRecoveryCodes.Clear();
        return Result.Success();
    }

    /// <summary>Issues a short-lived challenge after a password has already been verified but a second
    /// factor is still required to complete sign-in.</summary>
    public Result<string> IssueTwoFactorChallenge(string challengeTokenHash, DateTimeOffset expiresOnUtc)
    {
        if (!TwoFactorEnabled)
            return Result.Failure<string>(UserErrors.TwoFactorNotEnabled);

        TwoFactorChallengeTokenHash = challengeTokenHash;
        TwoFactorChallengeTokenExpiresOnUtc = expiresOnUtc;
        return Result.Success(challengeTokenHash);
    }

    /// <summary>Validates and consumes the pending 2FA challenge (single use), clearing it regardless of the
    /// outcome so a stale challenge can never be replayed.</summary>
    public Result CompleteTwoFactorChallenge(string challengeTokenHash, DateTimeOffset utcNow)
    {
        var hash = TwoFactorChallengeTokenHash;
        var expiry = TwoFactorChallengeTokenExpiresOnUtc;
        TwoFactorChallengeTokenHash = null;
        TwoFactorChallengeTokenExpiresOnUtc = null;

        if (hash is null || expiry is null || expiry.Value <= utcNow)
            return Result.Failure(UserErrors.TwoFactorChallengeRequired);

        if (!string.Equals(hash, challengeTokenHash, StringComparison.Ordinal))
            return Result.Failure(UserErrors.TwoFactorChallengeRequired);

        return Result.Success();
    }

    /// <summary>Consumes a recovery code (single use) as an alternative to a TOTP code, e.g. when the user
    /// has lost their authenticator device.</summary>
    public Result ConsumeTwoFactorRecoveryCode(string codeHash)
    {
        var recoveryCode = _twoFactorRecoveryCodes.FirstOrDefault(c => c.CodeHash == codeHash && !c.IsUsed);
        if (recoveryCode is null)
            return Result.Failure(UserErrors.TwoFactorCodeInvalid);

        recoveryCode.MarkUsed();
        return Result.Success();
    }

    public void SetCreated(DateTimeOffset onUtc, string? by)
    {
        CreatedOnUtc = onUtc;
        CreatedBy = by;
    }

    public void SetModified(DateTimeOffset onUtc, string? by)
    {
        ModifiedOnUtc = onUtc;
        ModifiedBy = by;
    }

    public void Delete(DateTimeOffset onUtc, string? by)
    {
        IsDeleted = true;
        DeletedOnUtc = onUtc;
        DeletedBy = by;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
    }
}
