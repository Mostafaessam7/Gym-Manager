using GymManager.Domain.Common;
using GymManager.Domain.Payments;
using Xunit;

namespace GymManager.UnitTests.Payments;

public sealed class PaymentTests
{
    private static Payment CreatePayment() =>
        Payment.Create(Guid.NewGuid(), Guid.NewGuid(), Money.Create(50m).Value, PaymentMethod.Cash, PaymentReferenceType.Other, null, null);

    [Fact]
    public void Complete_Should_Transition_From_Pending_To_Completed()
    {
        var payment = CreatePayment();

        var result = payment.Complete();

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.NotNull(payment.CompletedOnUtc);
    }

    [Fact]
    public void Complete_Should_Fail_When_Not_Pending()
    {
        var payment = CreatePayment();
        payment.Complete();

        var result = payment.Complete();

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.NotPending", result.Error.Code);
    }

    [Fact]
    public void Refund_Should_Fail_When_Not_Completed()
    {
        var payment = CreatePayment();

        var result = payment.Refund();

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.NotCompleted", result.Error.Code);
    }

    [Fact]
    public void Refund_Should_Succeed_After_Completion()
    {
        var payment = CreatePayment();
        payment.Complete();

        var result = payment.Refund();

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
    }

    [Fact]
    public void New_Payment_Should_Default_To_No_Gateway()
    {
        var payment = CreatePayment();

        Assert.Equal(PaymentGatewayProvider.None, payment.GatewayProvider);
        Assert.Null(payment.GatewayReferenceId);
    }

    [Fact]
    public void AttachGatewayReference_Should_Set_Provider_And_ReferenceId()
    {
        var payment = CreatePayment();

        payment.AttachGatewayReference(PaymentGatewayProvider.Stripe, "pi_test_123");

        Assert.Equal(PaymentGatewayProvider.Stripe, payment.GatewayProvider);
        Assert.Equal("pi_test_123", payment.GatewayReferenceId);
    }

    [Fact]
    public void Fail_Should_Transition_From_Pending_To_Failed()
    {
        var payment = CreatePayment();

        var result = payment.Fail();

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
    }
}
