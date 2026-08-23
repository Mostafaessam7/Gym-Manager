using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Expenses;
using GymManager.Domain.Expenses.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Expenses.UpdateExpense;

public sealed class UpdateExpenseCommandHandler(
    IExpenseRepository expenseRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<UpdateExpenseCommand>
{
    public async Task<Result> Handle(UpdateExpenseCommand command, CancellationToken cancellationToken)
    {
        var expense = await expenseRepository.GetByIdAsync(command.ExpenseId, cancellationToken);
        if (expense is null)
            return Result.Failure(ExpenseErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(expense.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        var amountResult = Money.Create(command.Amount, command.Currency);
        if (amountResult.IsFailure)
            return amountResult;

        expense.Update(command.Category, command.Description, amountResult.Value, command.ExpenseDate, command.PaidTo, command.ReceiptUrl);

        expenseRepository.Update(expense);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
