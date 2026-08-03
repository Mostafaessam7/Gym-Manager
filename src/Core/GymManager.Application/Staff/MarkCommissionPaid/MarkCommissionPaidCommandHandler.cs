using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity;
using GymManager.Domain.Staff;
using GymManager.Domain.Staff.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Staff.MarkCommissionPaid;

public sealed class MarkCommissionPaidCommandHandler(
    ICommissionRepository commissionRepository, IUserRepository userRepository,
    IBranchAccessGuard branchAccessGuard, IUnitOfWork unitOfWork)
    : ICommandHandler<MarkCommissionPaidCommand>
{
    public async Task<Result> Handle(MarkCommissionPaidCommand command, CancellationToken cancellationToken)
    {
        var commission = await commissionRepository.GetByIdAsync(command.CommissionId, cancellationToken);
        if (commission is null)
            return Result.Failure(StaffErrors.CommissionNotFound);

        var staffUser = await userRepository.GetByIdAsync(commission.UserId, cancellationToken);
        if (staffUser?.BranchId is { } staffBranchId)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(staffBranchId);
            if (accessResult.IsFailure)
                return accessResult;
        }

        var result = commission.MarkPaid(DateTimeOffset.UtcNow);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
