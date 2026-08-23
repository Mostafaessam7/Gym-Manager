using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Attendance;
using GymManager.Domain.Attendance.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Attendance.CheckOut;

public sealed class CheckOutCommandHandler(
    IAttendanceRepository attendanceRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<CheckOutCommand>
{
    public async Task<Result> Handle(CheckOutCommand command, CancellationToken cancellationToken)
    {
        var openSession = await attendanceRepository.GetOpenSessionByMemberIdAsync(command.MemberId, cancellationToken);
        if (openSession is null)
            return Result.Failure(AttendanceErrors.NoOpenSession);

        var accessResult = branchAccessGuard.EnsureCanAccess(openSession.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        var result = openSession.CheckOut();
        if (result.IsFailure)
            return result;

        attendanceRepository.Update(openSession);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
