using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Payments;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Payments.HandleStripeWebhook;

/// <summary>Applies a Stripe webhook event to the matching <c>Payment</c>. Deliberately tolerant of
/// conditions that are expected in normal operation rather than errors: Stripe delivers webhooks
/// at-least-once (so a redelivered event for an already-completed payment is a no-op, not a failure), and a
/// webhook for a PaymentIntent this system never created (a stray/test event) is likewise a silent no-op —
/// both cases still return success so Stripe doesn't retry indefinitely. Only a bad signature or a malformed
/// payload is treated as a real failure.</summary>
public sealed class HandleStripeWebhookCommandHandler(
    IPaymentGatewayService paymentGatewayService, IPaymentRepository paymentRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<HandleStripeWebhookCommand>
{
    public async Task<Result> Handle(HandleStripeWebhookCommand command, CancellationToken cancellationToken)
    {
        var eventResult = paymentGatewayService.ParseWebhookEvent(command.Payload, command.SignatureHeader);
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
