using GymManager.Application.Abstractions;
using GymManager.Application.Attendance.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Attendance.GetAttendanceRecords;

public sealed class GetAttendanceRecordsQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetAttendanceRecordsQuery, PagedList<AttendanceRecordResponse>>
{
    public async Task<PagedList<AttendanceRecordResponse>> Handle(GetAttendanceRecordsQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var records = readDb.AttendanceRecords.AsQueryable();

        var branchId = branchAccessGuard.ResolveFilter(query.BranchId);
        if (branchId.HasValue)
            records = records.Where(r => r.BranchId == branchId);

        if (query.MemberId.HasValue)
            records = records.Where(r => r.MemberId == query.MemberId);

        if (query.From.HasValue)
        {
            var from = query.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            records = records.Where(r => r.CheckInUtc >= from);
        }

        if (query.To.HasValue)
        {
            var to = query.To.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            records = records.Where(r => r.CheckInUtc <= to);
        }

        var totalCount = await records.CountAsync(cancellationToken);

        var page = await records
            .OrderByDescending(r => r.CheckInUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var memberIds = page.Select(r => r.MemberId).Distinct().ToArray();
        var memberNames = await readDb.Members
            .Where(m => memberIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => $"{m.FirstName} {m.LastName}", cancellationToken);

        var items = page
            .Select(r => new AttendanceRecordResponse(
                r.Id, r.MemberId, memberNames.GetValueOrDefault(r.MemberId, "Unknown"), r.BranchId,
                r.Method.ToString(), r.CheckInUtc, r.CheckOutUtc))
            .ToList();

        return new PagedList<AttendanceRecordResponse>(items, pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
