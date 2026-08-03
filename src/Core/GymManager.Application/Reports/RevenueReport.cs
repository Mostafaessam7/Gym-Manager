using GymManager.Application.Abstractions;
using GymManager.Domain.Payments;
using GymManager.SharedKernel.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Reports;

public sealed record RevenueReportRow(DateOnly Date, decimal TotalAmount, string Currency, int PaymentCount);

public sealed record RevenueReportQuery(Guid? BranchId, DateOnly From, DateOnly To) : IQuery<IReadOnlyList<RevenueReportRow>>;

public sealed class RevenueReportQueryHandler(IApplicationReadDb readDb) : IQueryHandler<RevenueReportQuery, IReadOnlyList<RevenueReportRow>>
{
    public async Task<IReadOnlyList<RevenueReportRow>> Handle(RevenueReportQuery query, CancellationToken cancellationToken)
    {
        var from = query.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = query.To.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var payments = readDb.Payments.Where(p => p.Status == PaymentStatus.Completed && p.CreatedOnUtc >= from && p.CreatedOnUtc <= to);

        if (query.BranchId.HasValue)
            payments = payments.Where(p => p.BranchId == query.BranchId);

        var rows = await payments
            .Select(p => new { p.CreatedOnUtc, p.Amount.Amount, p.Amount.Currency })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(p => new { Date = DateOnly.FromDateTime(p.CreatedOnUtc.UtcDateTime), p.Currency })
            .Select(g => new RevenueReportRow(g.Key.Date, g.Sum(x => x.Amount), g.Key.Currency, g.Count()))
            .OrderBy(r => r.Date)
            .ToList();
    }
}
