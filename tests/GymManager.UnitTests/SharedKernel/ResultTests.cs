using GymManager.SharedKernel.Results;
using Xunit;

namespace GymManager.UnitTests.SharedKernel;

public sealed class ResultTests
{
    [Fact]
    public void Success_Should_Have_No_Error()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_Should_Carry_The_Given_Error()
    {
        var error = Error.NotFound("Member.NotFound", "The member was not found.");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Creating_A_Failure_Result_Without_An_Error_Should_Throw()
    {
        Assert.Throws<InvalidOperationException>(() => Result.Failure(Error.None));
    }

    [Fact]
    public void Accessing_Value_Of_A_Failed_Result_Should_Throw()
    {
        var result = Result.Failure<int>(Error.Validation("Code", "Message"));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Implicit_Conversion_From_Value_Should_Produce_A_Successful_Result()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }
}
