using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Staff;
using GymManager.Domain.Staff.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Staff.MarkShiftNoShow;

public sealed class MarkShiftNoShowCommandHandler(IStaffShiftRepository staffShiftRepository, IBranchAccessGuard branchAccessGuard, IUnitOfWork unitOfWork)
    : ICommandHandler<MarkShiftNoShowCommand>
{
    public async Task<Result> Handle(MarkShiftNoShowCommand command, CancellationToken cancellationToken)
    {
        var shift = await staffShiftRepository.GetByIdAsync(command.ShiftId, cancellationToken);
        if (shift is null)
            return Result.Failure(StaffErrors.ShiftNotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(shift.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        var result = shift.MarkNoShow();
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
