using GymManager.Application.Abstractions;
using GymManager.Application.Payments.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Payments;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Payments.RecordPayment;

public sealed class RecordPaymentCommandHandler(
    IPaymentRepository paymentRepository, ICurrentUserService currentUserService, IUnitOfWork unitOfWork,
    IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<RecordPaymentCommand, Result<PaymentResponse>>
{
    public async Task<Result<PaymentResponse>> Handle(RecordPaymentCommand command, CancellationToken cancellationToken)
    {
        var accessResult = branchAccessGuard.EnsureCanAccess(command.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<PaymentResponse>(accessResult.Error);

        var amountResult = Money.Create(command.Amount, command.Currency);
        if (amountResult.IsFailure)
            return Result.Failure<PaymentResponse>(amountResult.Error);

        var payment = Payment.Create(
            command.MemberId, command.BranchId, amountResult.Value, command.Method,
            command.ReferenceType, command.ReferenceId, currentUserService.UserId);

        payment.Complete();

        paymentRepository.Add(payment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(payment.ToResponse());
    }
}
