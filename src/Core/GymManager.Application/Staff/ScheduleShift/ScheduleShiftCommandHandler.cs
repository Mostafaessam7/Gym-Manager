using GymManager.Application.Abstractions;
using GymManager.Application.Staff.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Identity.Errors;
using GymManager.Domain.Staff;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Staff.ScheduleShift;

public sealed class ScheduleShiftCommandHandler(
    IApplicationReadDb readDb, IStaffShiftRepository staffShiftRepository, IBranchAccessGuard branchAccessGuard, IUnitOfWork unitOfWork)
    : ICommandHandler<ScheduleShiftCommand, Result<StaffShiftResponse>>
{
    public async Task<Result<StaffShiftResponse>> Handle(ScheduleShiftCommand command, CancellationToken cancellationToken)
    {
        var userExists = await readDb.Users.AnyAsync(u => u.Id == command.UserId, cancellationToken);
        if (!userExists)
            return Result.Failure<StaffShiftResponse>(UserErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(command.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<StaffShiftResponse>(accessResult.Error);

        var shiftResult = StaffShift.Schedule(command.UserId, command.BranchId, command.StartUtc, command.EndUtc, command.Notes);
        if (shiftResult.IsFailure)
            return Result.Failure<StaffShiftResponse>(shiftResult.Error);

        staffShiftRepository.Add(shiftResult.Value);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(shiftResult.Value.ToResponse());
    }
}
