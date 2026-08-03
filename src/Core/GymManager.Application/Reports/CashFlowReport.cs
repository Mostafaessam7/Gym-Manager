using GymManager.Application.Abstractions;
using GymManager.Domain.Payments;
using GymManager.SharedKernel.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Reports;

public sealed record CashFlowRow(DateOnly Date, decimal Inflow, decimal Outflow, decimal NetChange, decimal RunningBalance);

public sealed record CashFlowReportQuery(Guid? BranchId, DateOnly From, DateOnly To) : IQuery<IReadOnlyList<CashFlowRow>>;

public sealed class CashFlowReportQueryHandler(IApplicationReadDb readDb) : IQueryHandler<CashFlowReportQuery, IReadOnlyList<CashFlowRow>>
{
    public async Task<IReadOnlyList<CashFlowRow>> Handle(CashFlowReportQuery query, CancellationToken cancellationToken)
    {
        var from = query.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = query.To.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var payments = readDb.Payments.Where(p => p.Status == PaymentStatus.Completed && p.CreatedOnUtc >= from && p.CreatedOnUtc <= to);
        if (query.BranchId.HasValue)
            payments = payments.Where(p => p.BranchId == query.BranchId);

        var expenses = readDb.Expenses.Where(e => e.ExpenseDate >= query.From && e.ExpenseDate <= query.To);
        if (query.BranchId.HasValue)
            expenses = expenses.Where(e => e.BranchId == query.BranchId);

        var inflows = await payments
            .Select(p => new { Date = DateOnly.FromDateTime(p.CreatedOnUtc.UtcDateTime), p.Amount.Amount })
            .ToListAsync(cancellationToken);

        var outflows = await expenses
            .Select(e => new { Date = e.ExpenseDate, e.Amount.Amount })
            .ToListAsync(cancellationToken);

        var allDates = inflows.Select(i => i.Date).Concat(outflows.Select(o => o.Date)).Distinct().OrderBy(d => d).ToList();

        var runningBalance = 0m;
        var rows = new List<CashFlowRow>();

        foreach (var date in allDates)
        {
            var inflow = inflows.Where(i => i.Date == date).Sum(i => i.Amount);
            var outflow = outflows.Where(o => o.Date == date).Sum(o => o.Amount);
            var netChange = inflow - outflow;
            runningBalance += netChange;

            rows.Add(new CashFlowRow(date, inflow, outflow, netChange, runningBalance));
        }

        return rows;
    }
}
