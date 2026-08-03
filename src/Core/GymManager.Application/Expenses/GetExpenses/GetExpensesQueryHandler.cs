using GymManager.Application.Abstractions;
using GymManager.Application.Expenses.Contracts;
using GymManager.Domain.Expenses;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Expenses.GetExpenses;

public sealed class GetExpensesQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetExpensesQuery, PagedList<ExpenseResponse>>
{
    public async Task<PagedList<ExpenseResponse>> Handle(GetExpensesQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var expenses = readDb.Expenses.AsQueryable();

        var branchId = branchAccessGuard.ResolveFilter(query.BranchId);
        if (branchId.HasValue)
            expenses = expenses.Where(e => e.BranchId == branchId);

        if (!string.IsNullOrWhiteSpace(query.Category) && Enum.TryParse<ExpenseCategory>(query.Category, true, out var category))
            expenses = expenses.Where(e => e.Category == category);

        if (query.From.HasValue)
            expenses = expenses.Where(e => e.ExpenseDate >= query.From);

        if (query.To.HasValue)
            expenses = expenses.Where(e => e.ExpenseDate <= query.To);

        var totalCount = await expenses.CountAsync(cancellationToken);

        var items = await expenses
            .OrderByDescending(e => e.ExpenseDate)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<ExpenseResponse>(items.Select(e => e.ToResponse()).ToList(), pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
