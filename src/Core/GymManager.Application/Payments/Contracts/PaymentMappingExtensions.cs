using GymManager.Domain.Payments;

namespace GymManager.Application.Payments.Contracts;

public static class PaymentMappingExtensions
{
    public static PaymentResponse ToResponse(this Payment payment) => new(
        payment.Id, payment.MemberId, payment.BranchId, payment.Amount.Amount, payment.Amount.Currency,
        payment.Method.ToString(), payment.Status.ToString(), payment.ReferenceType.ToString(), payment.ReferenceId,
        payment.ProcessedByUserId, payment.CompletedOnUtc, payment.GatewayProvider.ToString(), payment.GatewayReferenceId,
        payment.CreatedOnUtc);
}
