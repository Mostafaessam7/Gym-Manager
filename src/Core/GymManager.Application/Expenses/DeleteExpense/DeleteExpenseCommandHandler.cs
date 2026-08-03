using GymManager.Domain.Abstractions;
using GymManager.Domain.Expenses;
using GymManager.Domain.Expenses.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Expenses.DeleteExpense;

public sealed class DeleteExpenseCommandHandler(IExpenseRepository expenseRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteExpenseCommand>
{
    public async Task<Result> Handle(DeleteExpenseCommand command, CancellationToken cancellationToken)
    {
        var expense = await expenseRepository.GetByIdAsync(command.ExpenseId, cancellationToken);
        if (expense is null)
            return Result.Failure(ExpenseErrors.NotFound);

        expenseRepository.Remove(expense);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
