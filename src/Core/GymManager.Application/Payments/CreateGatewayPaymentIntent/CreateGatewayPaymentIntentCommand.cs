using GymManager.Application.Payments.Contracts;
using GymManager.Domain.Payments;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Payments.CreateGatewayPaymentIntent;

/// <summary>Starts a card payment through the configured gateway. The resulting <c>Payment</c> is created
/// <see cref="PaymentStatus.Pending"/> and only transitions to <see cref="PaymentStatus.Completed"/> once the
/// gateway's webhook confirms it — unlike <c>RecordPaymentCommand</c>, which is for payments already known to
/// have settled (cash, a manually-recorded bank transfer).</summary>
public sealed record CreateGatewayPaymentIntentCommand(
    Guid MemberId,
    Guid BranchId,
    decimal Amount,
    string Currency,
    PaymentReferenceType ReferenceType,
    Guid? ReferenceId,
    string? ReceiptEmail) : ICommand<Result<PaymentGatewayIntentResponse>>;
