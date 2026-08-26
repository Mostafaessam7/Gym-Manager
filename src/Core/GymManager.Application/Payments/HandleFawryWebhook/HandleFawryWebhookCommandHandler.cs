using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Payments;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Payments.HandleFawryWebhook;

/// <summary>Applies a FawryPay notification callback to the matching <c>Payment</c> — same tolerant
/// at-least-once/unknown-reference handling as <c>HandleStripeWebhookCommandHandler</c>, see its remarks.</summary>
public sealed class HandleFawryWebhookCommandHandler(
    IPaymentGatewayServiceResolver paymentGatewayServiceResolver, IPaymentRepository paymentRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<HandleFawryWebhookCommand>
{
    public async Task<Result> Handle(HandleFawryWebhookCommand command, CancellationToken cancellationToken)
    {
        var gatewayResult = paymentGatewayServiceResolver.Resolve(PaymentGatewayProvider.Fawry);
        if (gatewayResult.IsFailure)
            return gatewayResult;

        var eventResult = gatewayResult.Value.ParseWebhookEvent(command.Payload, command.Signature);
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
