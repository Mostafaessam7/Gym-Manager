using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Payments;
using GymManager.Domain.Payments.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Payments.RefundPayment;

public sealed class RefundPaymentCommandHandler(
    IPaymentRepository paymentRepository, IPaymentGatewayService paymentGatewayService,
    IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<RefundPaymentCommand>
{
    public async Task<Result> Handle(RefundPaymentCommand command, CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.GetByIdAsync(command.PaymentId, cancellationToken);
        if (payment is null)
            return Result.Failure(PaymentErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(payment.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        // Ask the gateway to reverse the charge before flipping local state, so a gateway-side failure
        // (network issue, already refunded there, etc.) doesn't leave this system believing money moved
        // when it didn't.
        if (payment.GatewayProvider != PaymentGatewayProvider.None && payment.GatewayReferenceId is not null)
        {
            var gatewayResult = await paymentGatewayService.RefundAsync(payment.GatewayReferenceId, amount: null, cancellationToken);
            if (gatewayResult.IsFailure)
                return Result.Failure(gatewayResult.Error);
        }

        var result = payment.Refund();
        if (result.IsFailure)
            return result;

        paymentRepository.Update(payment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
