using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Expenses.Errors;

public static class ExpenseErrors
{
    public static readonly Error NotFound = Error.NotFound("Expense.NotFound", "The expense was not found.");
}
