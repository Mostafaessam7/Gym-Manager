using GymManager.Domain.Attendance;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class AttendanceRepository(GymManagerDbContext dbContext) : IAttendanceRepository
{
    public Task<AttendanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.AttendanceRecords.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<AttendanceRecord?> GetOpenSessionByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        dbContext.AttendanceRecords.FirstOrDefaultAsync(a => a.MemberId == memberId && a.CheckOutUtc == null, cancellationToken);

    public Task<int> GetCheckInCountForBranchOnDateAsync(Guid branchId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        return dbContext.AttendanceRecords.CountAsync(
            a => a.BranchId == branchId && a.CheckInUtc >= start && a.CheckInUtc <= end, cancellationToken);
    }

    public void Add(AttendanceRecord aggregate) => dbContext.AttendanceRecords.Add(aggregate);

    public void Update(AttendanceRecord aggregate) => dbContext.AttendanceRecords.Update(aggregate);

    public void Remove(AttendanceRecord aggregate) => dbContext.AttendanceRecords.Remove(aggregate);
}
