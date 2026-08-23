using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Expenses;
using GymManager.Domain.Expenses.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Expenses.DeleteExpense;

public sealed class DeleteExpenseCommandHandler(
    IExpenseRepository expenseRepository, IUnitOfWork unitOfWork, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<DeleteExpenseCommand>
{
    public async Task<Result> Handle(DeleteExpenseCommand command, CancellationToken cancellationToken)
    {
        var expense = await expenseRepository.GetByIdAsync(command.ExpenseId, cancellationToken);
        if (expense is null)
            return Result.Failure(ExpenseErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(expense.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        expenseRepository.Remove(expense);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
