using GymManager.Application.Abstractions;
using GymManager.Application.Attendance.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Attendance;
using GymManager.Domain.Attendance.Errors;
using GymManager.Domain.Members;
using GymManager.Domain.Memberships;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Attendance.CheckIn;

public sealed class CheckInCommandHandler(
    IMemberRepository memberRepository,
    IMembershipRepository membershipRepository,
    IAttendanceRepository attendanceRepository,
    IUnitOfWork unitOfWork,
    IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<CheckInCommand, Result<AttendanceRecordResponse>>
{
    public async Task<Result<AttendanceRecordResponse>> Handle(CheckInCommand command, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByCheckInCodeAsync(command.CheckInCode, cancellationToken);
        if (member is null)
            return Result.Failure<AttendanceRecordResponse>(AttendanceErrors.InvalidCheckInCode);

        var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<AttendanceRecordResponse>(accessResult.Error);

        if (member.Status != MemberStatus.Active)
            return Result.Failure<AttendanceRecordResponse>(AttendanceErrors.MemberNotActive);

        var activeMembership = await membershipRepository.GetActiveByMemberIdAsync(member.Id, cancellationToken);
        if (activeMembership is null || !activeMembership.IsCurrentlyActive(DateOnly.FromDateTime(DateTime.UtcNow)))
            return Result.Failure<AttendanceRecordResponse>(AttendanceErrors.MembershipNotActive);

        var openSession = await attendanceRepository.GetOpenSessionByMemberIdAsync(member.Id, cancellationToken);
        if (openSession is not null)
            return Result.Failure<AttendanceRecordResponse>(AttendanceErrors.AlreadyCheckedIn);

        var record = AttendanceRecord.CheckIn(member.Id, member.BranchId, command.Method);

        attendanceRepository.Add(record);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AttendanceRecordResponse(
            record.Id, member.Id, $"{member.FirstName} {member.LastName}", record.BranchId,
            record.Method.ToString(), record.CheckInUtc, record.CheckOutUtc));
    }
}
