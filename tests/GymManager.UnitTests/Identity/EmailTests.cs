using GymManager.Domain.Common;
using Xunit;

namespace GymManager.UnitTests.Identity;

public sealed class EmailTests
{
    [Theory]
    [InlineData("USER@Example.com", "user@example.com")]
    [InlineData("  trainer@gym.io  ", "trainer@gym.io")]
    public void Create_Should_Normalize_Valid_Emails(string input, string expected)
    {
        var result = Email.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_Should_Fail_When_Empty(string? input)
    {
        var result = Email.Create(input);

        Assert.True(result.IsFailure);
        Assert.Equal("Email.Empty", result.Error.Code);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData("@nouser.com")]
    public void Create_Should_Fail_When_Invalid_Format(string input)
    {
        var result = Email.Create(input);

        Assert.True(result.IsFailure);
        Assert.Equal("Email.Invalid", result.Error.Code);
    }
}
