using GymManager.Application.Identity.Login;
using Xunit;

namespace GymManager.UnitTests.Identity;

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Should_Fail_When_Email_Is_Empty()
    {
        var result = _validator.Validate(new LoginCommand(string.Empty, "Password123"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Fail_When_Password_Is_Empty()
    {
        var result = _validator.Validate(new LoginCommand("user@gym.io", string.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Pass_For_Valid_Input()
    {
        var result = _validator.Validate(new LoginCommand("user@gym.io", "Password123"));

        Assert.True(result.IsValid);
    }
}
