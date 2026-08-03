using GymManager.Application.Abstractions;
using GymManager.Domain.Classes;
using GymManager.SharedKernel.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Reports;

public sealed record TrainerReportRow(string TrainerName, string Specialization, int SessionCount, int TotalBookings, bool IsActive);

public sealed record TrainersReportQuery(Guid? BranchId, DateOnly From, DateOnly To) : IQuery<IReadOnlyList<TrainerReportRow>>;

public sealed class TrainersReportQueryHandler(IApplicationReadDb readDb) : IQueryHandler<TrainersReportQuery, IReadOnlyList<TrainerReportRow>>
{
    public async Task<IReadOnlyList<TrainerReportRow>> Handle(TrainersReportQuery query, CancellationToken cancellationToken)
    {
        var trainers = readDb.Trainers.AsQueryable();
        if (query.BranchId.HasValue)
            trainers = trainers.Where(t => t.BranchId == query.BranchId);

        var trainerList = await trainers.ToListAsync(cancellationToken);

        var from = query.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = query.To.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var sessions = await readDb.ClassSessions
            .Where(s => s.StartUtc >= from && s.StartUtc <= to)
            .Select(s => new { s.TrainerId, BookingCount = s.Bookings.Count(b => b.Status != BookingStatus.Cancelled) })
            .ToListAsync(cancellationToken);

        var sessionsByTrainer = sessions.GroupBy(s => s.TrainerId)
            .ToDictionary(g => g.Key, g => (SessionCount: g.Count(), BookingCount: g.Sum(x => x.BookingCount)));

        return trainerList
            .Select(t =>
            {
                var stats = sessionsByTrainer.GetValueOrDefault(t.Id, (SessionCount: 0, BookingCount: 0));
                return new TrainerReportRow($"{t.FirstName} {t.LastName}", t.Specialization, stats.SessionCount, stats.BookingCount, t.IsActive);
            })
            .OrderByDescending(r => r.TotalBookings)
            .ToList();
    }
}
