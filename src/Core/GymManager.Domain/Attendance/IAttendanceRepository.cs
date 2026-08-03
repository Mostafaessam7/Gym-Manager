using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Attendance;

public interface IAttendanceRepository : IRepository<AttendanceRecord, Guid>
{
    Task<AttendanceRecord?> GetOpenSessionByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task<int> GetCheckInCountForBranchOnDateAsync(Guid branchId, DateOnly date, CancellationToken cancellationToken = default);
}
