using GymManager.Application.Abstractions;
using GymManager.Application.Staff.Contracts;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Common;
using GymManager.Domain.Identity.Errors;
using GymManager.Domain.Staff;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Staff.RecordCommission;

public sealed class RecordCommissionCommandHandler(
    IApplicationReadDb readDb, ICommissionRepository commissionRepository, IBranchAccessGuard branchAccessGuard, IUnitOfWork unitOfWork)
    : ICommandHandler<RecordCommissionCommand, Result<CommissionResponse>>
{
    public async Task<Result<CommissionResponse>> Handle(RecordCommissionCommand command, CancellationToken cancellationToken)
    {
        var staffUser = await readDb.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);
        if (staffUser is null)
            return Result.Failure<CommissionResponse>(UserErrors.NotFound);

        if (staffUser.BranchId.HasValue)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(staffUser.BranchId.Value);
            if (accessResult.IsFailure)
                return Result.Failure<CommissionResponse>(accessResult.Error);
        }

        var amountResult = Money.Create(command.Amount);
        if (amountResult.IsFailure)
            return Result.Failure<CommissionResponse>(amountResult.Error);

        var commission = Commission.Record(
            command.UserId, amountResult.Value, command.SourceType, command.SourceReferenceId, command.EarnedOnUtc, command.Notes);

        commissionRepository.Add(commission);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(commission.ToResponse());
    }
}
