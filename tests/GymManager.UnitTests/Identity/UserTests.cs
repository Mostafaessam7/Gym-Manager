using GymManager.Domain.Common;
using GymManager.Domain.Identity;
using Xunit;

namespace GymManager.UnitTests.Identity;

public sealed class UserTests
{
    private static User CreateUser() =>
        User.Register(Email.Create("trainer@gym.io").Value, "hashed-password", "Jane", "Doe");

    [Fact]
    public void Register_Should_Raise_UserRegisteredDomainEvent()
    {
        var user = CreateUser();

        Assert.Single(user.DomainEvents);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void Deactivate_Should_Revoke_Active_Refresh_Tokens()
    {
        var user = CreateUser();
        user.IssueRefreshToken("token-1", DateTimeOffset.UtcNow.AddDays(7));

        user.Deactivate();

        Assert.False(user.IsActive);
        Assert.All(user.RefreshTokens, t => Assert.False(t.IsActive));
    }

    [Fact]
    public void VerifyIsActive_Should_Fail_For_Deactivated_User()
    {
        var user = CreateUser();
        user.Deactivate();

        var result = user.VerifyIsActive();

        Assert.True(result.IsFailure);
        Assert.Equal("User.AccountDeactivated", result.Error.Code);
    }

    [Fact]
    public void AssignRole_Should_Fail_When_Role_Already_Assigned()
    {
        var user = CreateUser();
        var roleId = Guid.NewGuid();
        user.AssignRole(roleId);

        var result = user.AssignRole(roleId);

        Assert.True(result.IsFailure);
        Assert.Equal("User.AlreadyInRole", result.Error.Code);
    }

    [Fact]
    public void RevokeRole_Should_Fail_When_User_Does_Not_Have_Role()
    {
        var user = CreateUser();

        var result = user.RevokeRole(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotInRole", result.Error.Code);
    }

    [Fact]
    public void RotateRefreshToken_Should_Revoke_Old_And_Issue_New_Token()
    {
        var user = CreateUser();
        user.IssueRefreshToken("old-token", DateTimeOffset.UtcNow.AddDays(7));

        var result = user.RotateRefreshToken("old-token", "new-token", DateTimeOffset.UtcNow.AddDays(7));

        Assert.True(result.IsSuccess);
        Assert.Equal("new-token", result.Value.TokenHash);
        Assert.False(user.RefreshTokens.Single(t => t.TokenHash == "old-token").IsActive);
    }

    [Fact]
    public void RotateRefreshToken_Should_Fail_For_Unknown_Token()
    {
        var user = CreateUser();

        var result = user.RotateRefreshToken("unknown", "new-token", DateTimeOffset.UtcNow.AddDays(7));

        Assert.True(result.IsFailure);
        Assert.Equal("User.RefreshTokenInvalid", result.Error.Code);
    }

    [Fact]
    public void RevokeSession_Should_Revoke_Only_The_Matching_Token()
    {
        var user = CreateUser();
        var kept = user.IssueRefreshToken("keep-me", DateTimeOffset.UtcNow.AddDays(7));
        var revoked = user.IssueRefreshToken("revoke-me", DateTimeOffset.UtcNow.AddDays(7));

        var result = user.RevokeSession(revoked.Id);

        Assert.True(result.IsSuccess);
        Assert.False(user.RefreshTokens.Single(t => t.Id == revoked.Id).IsActive);
        Assert.True(user.RefreshTokens.Single(t => t.Id == kept.Id).IsActive);
    }

    [Fact]
    public void RevokeSession_Should_Fail_For_Unknown_Session()
    {
        var user = CreateUser();

        var result = user.RevokeSession(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("User.RefreshTokenInvalid", result.Error.Code);
    }

    [Fact]
    public void RevokeAllSessions_Should_Revoke_Every_Active_Session_Except_The_One_Excluded()
    {
        var user = CreateUser();
        var current = user.IssueRefreshToken("current-session", DateTimeOffset.UtcNow.AddDays(7));
        var other = user.IssueRefreshToken("other-session", DateTimeOffset.UtcNow.AddDays(7));

        user.RevokeAllSessions(exceptSessionId: current.Id);

        Assert.True(user.RefreshTokens.Single(t => t.Id == current.Id).IsActive);
        Assert.False(user.RefreshTokens.Single(t => t.Id == other.Id).IsActive);
    }

    [Fact]
    public void ChangePassword_Should_Record_The_Outgoing_Hash_In_History_And_Cap_At_The_Limit()
    {
        var user = User.Register(Email.Create("history@gym.io").Value, "hash-0", "Jane", "Doe");

        for (var i = 1; i <= User.PasswordHistoryLimit + 2; i++)
            user.ChangePassword($"hash-{i}", DateTimeOffset.UtcNow);

        Assert.Equal(User.PasswordHistoryLimit, user.PasswordHistory.Count);
        Assert.DoesNotContain(user.PasswordHistory, e => e.PasswordHash == "hash-0");
        Assert.Contains(user.PasswordHistory, e => e.PasswordHash == $"hash-{User.PasswordHistoryLimit + 1}");
    }

    [Fact]
    public void VerifyEmail_With_Valid_Token_Should_Mark_The_Account_Verified_And_Clear_The_Token()
    {
        var user = CreateUser();
        user.SetEmailVerificationToken("token-hash", DateTimeOffset.UtcNow.AddHours(1));

        var result = user.VerifyEmail("token-hash", DateTimeOffset.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.True(user.IsEmailVerified);
        Assert.Null(user.EmailVerificationTokenHash);
    }

    [Fact]
    public void VerifyEmail_Should_Fail_When_Already_Verified()
    {
        var user = CreateUser();
        user.MarkEmailVerified();

        var result = user.VerifyEmail("any-token", DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("User.EmailAlreadyVerified", result.Error.Code);
    }

    [Fact]
    public void VerifyEmail_Should_Fail_When_The_Token_Has_Expired()
    {
        var user = CreateUser();
        user.SetEmailVerificationToken("token-hash", DateTimeOffset.UtcNow.AddHours(-1));

        var result = user.VerifyEmail("token-hash", DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("User.EmailVerificationTokenInvalid", result.Error.Code);
    }

    [Fact]
    public void VerifyEmail_Should_Fail_For_A_Mismatched_Token()
    {
        var user = CreateUser();
        user.SetEmailVerificationToken("token-hash", DateTimeOffset.UtcNow.AddHours(1));

        var result = user.VerifyEmail("wrong-hash", DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("User.EmailVerificationTokenInvalid", result.Error.Code);
    }

    [Fact]
    public void StartTwoFactorSetup_Should_Fail_When_Already_Enabled()
    {
        var user = CreateUser();
        user.StartTwoFactorSetup("secret-key");
        user.ConfirmTwoFactorSetup(["recovery-hash"]);

        var result = user.StartTwoFactorSetup("another-secret");

        Assert.True(result.IsFailure);
        Assert.Equal("User.TwoFactorAlreadyEnabled", result.Error.Code);
    }

    [Fact]
    public void ConfirmTwoFactorSetup_Should_Fail_Without_A_Pending_Setup()
    {
        var user = CreateUser();

        var result = user.ConfirmTwoFactorSetup(["recovery-hash"]);

        Assert.True(result.IsFailure);
        Assert.Equal("User.TwoFactorNotEnabled", result.Error.Code);
    }

    [Fact]
    public void ConfirmTwoFactorSetup_Should_Enable_TwoFactor_And_Store_Recovery_Codes()
    {
        var user = CreateUser();
        user.StartTwoFactorSetup("secret-key");

        var result = user.ConfirmTwoFactorSetup(["hash-1", "hash-2"]);

        Assert.True(result.IsSuccess);
        Assert.True(user.TwoFactorEnabled);
        Assert.Equal(2, user.TwoFactorRecoveryCodes.Count);
    }

    [Fact]
    public void DisableTwoFactor_Should_Clear_Secret_And_Recovery_Codes()
    {
        var user = CreateUser();
        user.StartTwoFactorSetup("secret-key");
        user.ConfirmTwoFactorSetup(["hash-1"]);

        var result = user.DisableTwoFactor();

        Assert.True(result.IsSuccess);
        Assert.False(user.TwoFactorEnabled);
        Assert.Null(user.TwoFactorSecretKey);
        Assert.Empty(user.TwoFactorRecoveryCodes);
    }

    [Fact]
    public void DisableTwoFactor_Should_Fail_When_Not_Enabled()
    {
        var user = CreateUser();

        var result = user.DisableTwoFactor();

        Assert.True(result.IsFailure);
        Assert.Equal("User.TwoFactorNotEnabled", result.Error.Code);
    }

    [Fact]
    public void IssueTwoFactorChallenge_Should_Fail_When_TwoFactor_Not_Enabled()
    {
        var user = CreateUser();

        var result = user.IssueTwoFactorChallenge("challenge-hash", DateTimeOffset.UtcNow.AddMinutes(5));

        Assert.True(result.IsFailure);
        Assert.Equal("User.TwoFactorNotEnabled", result.Error.Code);
    }

    [Fact]
    public void CompleteTwoFactorChallenge_Should_Succeed_For_A_Valid_Unexpired_Challenge()
    {
        var user = CreateUser();
        user.StartTwoFactorSetup("secret-key");
        user.ConfirmTwoFactorSetup(["hash-1"]);
        user.IssueTwoFactorChallenge("challenge-hash", DateTimeOffset.UtcNow.AddMinutes(5));

        var result = user.CompleteTwoFactorChallenge("challenge-hash", DateTimeOffset.UtcNow);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CompleteTwoFactorChallenge_Should_Fail_And_Clear_The_Challenge_When_Expired()
    {
        var user = CreateUser();
        user.StartTwoFactorSetup("secret-key");
        user.ConfirmTwoFactorSetup(["hash-1"]);
        user.IssueTwoFactorChallenge("challenge-hash", DateTimeOffset.UtcNow.AddMinutes(-1));

        var result = user.CompleteTwoFactorChallenge("challenge-hash", DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("User.TwoFactorChallengeRequired", result.Error.Code);

        var replay = user.CompleteTwoFactorChallenge("challenge-hash", DateTimeOffset.UtcNow);
        Assert.True(replay.IsFailure);
    }

    [Fact]
    public void ConsumeTwoFactorRecoveryCode_Should_Be_Single_Use()
    {
        var user = CreateUser();
        user.StartTwoFactorSetup("secret-key");
        user.ConfirmTwoFactorSetup(["recovery-hash"]);

        var firstUse = user.ConsumeTwoFactorRecoveryCode("recovery-hash");
        var secondUse = user.ConsumeTwoFactorRecoveryCode("recovery-hash");

        Assert.True(firstUse.IsSuccess);
        Assert.True(secondUse.IsFailure);
        Assert.Equal("User.TwoFactorCodeInvalid", secondUse.Error.Code);
    }
}
