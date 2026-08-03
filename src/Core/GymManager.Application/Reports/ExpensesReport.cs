using GymManager.Application.Abstractions;
using GymManager.SharedKernel.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Reports;

public sealed record ExpenseReportRow(DateOnly ExpenseDate, string Category, string Description, decimal Amount, string Currency, string PaidTo);

public sealed record ExpensesReportQuery(Guid? BranchId, DateOnly From, DateOnly To) : IQuery<IReadOnlyList<ExpenseReportRow>>;

public sealed class ExpensesReportQueryHandler(IApplicationReadDb readDb) : IQueryHandler<ExpensesReportQuery, IReadOnlyList<ExpenseReportRow>>
{
    public async Task<IReadOnlyList<ExpenseReportRow>> Handle(ExpensesReportQuery query, CancellationToken cancellationToken)
    {
        var expenses = readDb.Expenses.Where(e => e.ExpenseDate >= query.From && e.ExpenseDate <= query.To);
        if (query.BranchId.HasValue)
            expenses = expenses.Where(e => e.BranchId == query.BranchId);

        return await expenses
            .OrderBy(e => e.ExpenseDate)
            .Select(e => new ExpenseReportRow(e.ExpenseDate, e.Category.ToString(), e.Description, e.Amount.Amount, e.Amount.Currency, e.PaidTo))
            .ToListAsync(cancellationToken);
    }
}
