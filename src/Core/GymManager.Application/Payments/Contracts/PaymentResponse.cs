namespace GymManager.Application.Payments.Contracts;

public sealed record PaymentResponse(
    Guid Id,
    Guid MemberId,
    Guid BranchId,
    decimal Amount,
    string Currency,
    string Method,
    string Status,
    string ReferenceType,
    Guid? ReferenceId,
    Guid? ProcessedByUserId,
    DateTimeOffset? CompletedOnUtc,
    string GatewayProvider,
    string? GatewayReferenceId,
    DateTimeOffset CreatedOnUtc);

/// <summary>Returned from starting a gateway-backed payment — <see cref="ClientSecret"/> is what the
/// frontend passes to Stripe.js/Stripe Elements to collect card details and confirm the payment client-side;
/// it is never persisted server-side beyond this one response.</summary>
public sealed record PaymentGatewayIntentResponse(Guid PaymentId, string ClientSecret, string PublishableKey);
