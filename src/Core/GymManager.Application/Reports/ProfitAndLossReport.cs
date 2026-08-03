using GymManager.Application.Abstractions;
using GymManager.Domain.Payments;
using GymManager.SharedKernel.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Reports;

public sealed record ProfitAndLossRow(decimal TotalRevenue, decimal TotalExpenses, decimal NetProfit, string Currency);

public sealed record ProfitAndLossReportQuery(Guid? BranchId, DateOnly From, DateOnly To) : IQuery<ProfitAndLossRow>;

public sealed class ProfitAndLossReportQueryHandler(IApplicationReadDb readDb) : IQueryHandler<ProfitAndLossReportQuery, ProfitAndLossRow>
{
    public async Task<ProfitAndLossRow> Handle(ProfitAndLossReportQuery query, CancellationToken cancellationToken)
    {
        var from = query.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = query.To.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var payments = readDb.Payments.Where(p => p.Status == PaymentStatus.Completed && p.CreatedOnUtc >= from && p.CreatedOnUtc <= to);
        if (query.BranchId.HasValue)
            payments = payments.Where(p => p.BranchId == query.BranchId);

        var expenses = readDb.Expenses.Where(e => e.ExpenseDate >= query.From && e.ExpenseDate <= query.To);
        if (query.BranchId.HasValue)
            expenses = expenses.Where(e => e.BranchId == query.BranchId);

        var revenueData = await payments.Select(p => new { p.Amount.Amount, p.Amount.Currency }).ToListAsync(cancellationToken);
        var expenseData = await expenses.Select(e => e.Amount.Amount).ToListAsync(cancellationToken);

        var totalRevenue = revenueData.Sum(p => p.Amount);
        var totalExpenses = expenseData.Sum();
        var currency = revenueData.Count > 0 ? revenueData[0].Currency : "USD";

        return new ProfitAndLossRow(totalRevenue, totalExpenses, totalRevenue - totalExpenses, currency);
    }
}
