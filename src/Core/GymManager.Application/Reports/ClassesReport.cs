using GymManager.Application.Abstractions;
using GymManager.Domain.Classes;
using GymManager.SharedKernel.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Reports;

public sealed record ClassReportRow(string ClassName, int SessionCount, int TotalBookings, int Capacity, double AverageFillRate);

public sealed record ClassesReportQuery(Guid? BranchId, DateOnly From, DateOnly To) : IQuery<IReadOnlyList<ClassReportRow>>;

public sealed class ClassesReportQueryHandler(IApplicationReadDb readDb) : IQueryHandler<ClassesReportQuery, IReadOnlyList<ClassReportRow>>
{
    public async Task<IReadOnlyList<ClassReportRow>> Handle(ClassesReportQuery query, CancellationToken cancellationToken)
    {
        var from = query.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = query.To.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var sessions = readDb.ClassSessions.Where(s => s.StartUtc >= from && s.StartUtc <= to);
        if (query.BranchId.HasValue)
            sessions = sessions.Where(s => s.BranchId == query.BranchId);

        var rows = await sessions
            .Select(s => new
            {
                s.GymClassId, s.Capacity,
                BookingCount = s.Bookings.Count(b => b.Status != BookingStatus.Cancelled),
            })
            .ToListAsync(cancellationToken);

        var classIds = rows.Select(r => r.GymClassId).Distinct().ToArray();
        var classNames = await readDb.GymClasses.Where(c => classIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        return rows.GroupBy(r => r.GymClassId)
            .Select(g => new ClassReportRow(
                classNames.GetValueOrDefault(g.Key, "Unknown"),
                g.Count(),
                g.Sum(x => x.BookingCount),
                g.Sum(x => x.Capacity),
                g.Sum(x => x.Capacity) == 0 ? 0 : Math.Round(g.Sum(x => x.BookingCount) * 100d / g.Sum(x => x.Capacity), 1)))
            .OrderByDescending(r => r.TotalBookings)
            .ToList();
    }
}
