using GymManager.Application.Abstractions;
using GymManager.SharedKernel.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Reports;

public sealed record AttendanceReportRow(string MemberName, DateTimeOffset CheckInUtc, DateTimeOffset? CheckOutUtc, string Method);

public sealed record AttendanceReportQuery(Guid? BranchId, DateOnly From, DateOnly To) : IQuery<IReadOnlyList<AttendanceReportRow>>;

public sealed class AttendanceReportQueryHandler(IApplicationReadDb readDb) : IQueryHandler<AttendanceReportQuery, IReadOnlyList<AttendanceReportRow>>
{
    public async Task<IReadOnlyList<AttendanceReportRow>> Handle(AttendanceReportQuery query, CancellationToken cancellationToken)
    {
        var from = query.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = query.To.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var records = readDb.AttendanceRecords.Where(a => a.CheckInUtc >= from && a.CheckInUtc <= to);

        if (query.BranchId.HasValue)
            records = records.Where(a => a.BranchId == query.BranchId);

        var rows = await records
            .OrderBy(a => a.CheckInUtc)
            .Select(a => new { a.MemberId, a.CheckInUtc, a.CheckOutUtc, a.Method })
            .ToListAsync(cancellationToken);

        var memberIds = rows.Select(r => r.MemberId).Distinct().ToArray();
        var memberNames = await readDb.Members.Where(m => memberIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => $"{m.FirstName} {m.LastName}", cancellationToken);

        return rows
            .Select(r => new AttendanceReportRow(memberNames.GetValueOrDefault(r.MemberId, "Unknown"), r.CheckInUtc, r.CheckOutUtc, r.Method.ToString()))
            .ToList();
    }
}
