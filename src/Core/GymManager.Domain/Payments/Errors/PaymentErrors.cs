using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Payments.Errors;

public static class PaymentErrors
{
    public static readonly Error NotFound = Error.NotFound("Payment.NotFound", "The payment was not found.");

    public static readonly Error NotPending = Error.Conflict("Payment.NotPending", "Only a pending payment can be completed or failed.");

    public static readonly Error NotCompleted = Error.Conflict("Payment.NotCompleted", "Only a completed payment can be refunded.");

    public static readonly Error NotGatewayBacked = Error.Validation(
        "Payment.NotGatewayBacked", "This payment was not collected through a payment gateway.");

    public static Error GatewayRequestFailed(string reason) =>
        Error.Failure("Payment.GatewayRequestFailed", $"The payment gateway rejected the request: {reason}");

    public static readonly Error WebhookEventUnrecognized = Error.Validation(
        "Payment.WebhookEventUnrecognized", "The webhook payload could not be matched to a known payment.");

    public static Error WebhookSignatureInvalid(string reason) =>
        Error.Validation("Payment.WebhookSignatureInvalid", $"The webhook signature could not be verified: {reason}");
}
