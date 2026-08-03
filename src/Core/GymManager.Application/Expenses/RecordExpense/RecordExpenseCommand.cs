using GymManager.Application.Expenses.Contracts;
using GymManager.Domain.Expenses;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Expenses.RecordExpense;

public sealed record RecordExpenseCommand(
    Guid BranchId, ExpenseCategory Category, string Description, decimal Amount, string Currency,
    DateOnly ExpenseDate, string PaidTo, string? ReceiptUrl) : ICommand<Result<ExpenseResponse>>;
