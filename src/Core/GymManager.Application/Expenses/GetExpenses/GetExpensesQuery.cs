using GymManager.Application.Expenses.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Expenses.GetExpenses;

public sealed record GetExpensesQuery(PaginationParameters Pagination, Guid? BranchId, string? Category, DateOnly? From, DateOnly? To)
    : IQuery<PagedList<ExpenseResponse>>;
