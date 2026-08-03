using GymManager.Application.Abstractions;
using GymManager.Application.Expenses.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Expenses;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Expenses.RecordExpense;

public sealed class RecordExpenseCommandHandler(
    IExpenseRepository expenseRepository, ICurrentUserService currentUserService, IUnitOfWork unitOfWork,
    IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<RecordExpenseCommand, Result<ExpenseResponse>>
{
    public async Task<Result<ExpenseResponse>> Handle(RecordExpenseCommand command, CancellationToken cancellationToken)
    {
        var accessResult = branchAccessGuard.EnsureCanAccess(command.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<ExpenseResponse>(accessResult.Error);

        var amountResult = Money.Create(command.Amount, command.Currency);
        if (amountResult.IsFailure)
            return Result.Failure<ExpenseResponse>(amountResult.Error);

        var recordedBy = currentUserService.UserId ?? Guid.Empty;

        var expense = Expense.Record(
            command.BranchId, command.Category, command.Description, amountResult.Value, command.ExpenseDate,
            command.PaidTo, recordedBy, command.ReceiptUrl);

        expenseRepository.Add(expense);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(expense.ToResponse());
    }
}
