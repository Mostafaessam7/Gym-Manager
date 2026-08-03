using GymManager.Domain.Expenses;
using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Expenses.UpdateExpense;

public sealed record UpdateExpenseCommand(
    Guid ExpenseId, ExpenseCategory Category, string Description, decimal Amount, string Currency,
    DateOnly ExpenseDate, string PaidTo, string? ReceiptUrl) : ICommand;
