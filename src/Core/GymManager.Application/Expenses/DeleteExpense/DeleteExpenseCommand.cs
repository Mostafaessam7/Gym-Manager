using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Expenses.DeleteExpense;

public sealed record DeleteExpenseCommand(Guid ExpenseId) : ICommand;
