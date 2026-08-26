using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Payments;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Payments.HandlePaymobWebhook;

/// <summary>Applies a Paymob transaction-callback event to the matching <c>Payment</c> — same tolerant
/// at-least-once/unknown-reference handling as <c>HandleStripeWebhookCommandHandler</c>, see its remarks.</summary>
public sealed class HandlePaymobWebhookCommandHandler(
    IPaymentGatewayServiceResolver paymentGatewayServiceResolver, IPaymentRepository paymentRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<HandlePaymobWebhookCommand>
{
    public async Task<Result> Handle(HandlePaymobWebhookCommand command, CancellationToken cancellationToken)
    {
        var gatewayResult = paymentGatewayServiceResolver.Resolve(PaymentGatewayProvider.Paymob);
        if (gatewayResult.IsFailure)
            return gatewayResult;

        var eventResult = gatewayResult.Value.ParseWebhookEvent(command.Payload, command.Hmac);
        if (eventResult.IsFailure)
            return eventResult;

        var webhookEvent = eventResult.Value;

        var payment = await paymentRepository.GetByGatewayReferenceIdAsync(webhookEvent.GatewayReferenceId, cancellationToken);
        if (payment is null || payment.Status != PaymentStatus.Pending)
            return Result.Success();

        switch (webhookEvent.Outcome)
        {
            case PaymentGatewayEventOutcome.Succeeded:
                payment.Complete();
                // Swap to the transaction id: refunding this payment later needs it, and the order id
                // (used above to find this payment) has served its only purpose.
                if (webhookEvent.SecondaryReferenceId is not null)
                    payment.UpdateGatewayReference(webhookEvent.SecondaryReferenceId);
                break;
            case PaymentGatewayEventOutcome.Failed:
                payment.Fail();
                break;
            default:
                return Result.Success();
        }

        paymentRepository.Update(payment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
