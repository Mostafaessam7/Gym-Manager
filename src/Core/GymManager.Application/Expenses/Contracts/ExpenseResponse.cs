using GymManager.Domain.Expenses;

namespace GymManager.Application.Expenses.Contracts;

public sealed record ExpenseResponse(
    Guid Id,
    Guid BranchId,
    string Category,
    string Description,
    decimal Amount,
    string Currency,
    DateOnly ExpenseDate,
    string PaidTo,
    Guid RecordedByUserId,
    string? ReceiptUrl);

public static class ExpenseMappingExtensions
{
    public static ExpenseResponse ToResponse(this Expense expense) => new(
        expense.Id, expense.BranchId, expense.Category.ToString(), expense.Description, expense.Amount.Amount,
        expense.Amount.Currency, expense.ExpenseDate, expense.PaidTo, expense.RecordedByUserId, expense.ReceiptUrl);
}
