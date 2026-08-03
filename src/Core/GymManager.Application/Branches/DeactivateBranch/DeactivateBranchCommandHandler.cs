using GymManager.Application.Abstractions;
using GymManager.Application.Branches.GetBranches;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Branches;
using GymManager.Domain.Branches.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Branches.DeactivateBranch;

public sealed class DeactivateBranchCommandHandler(
    IBranchRepository branchRepository, IUnitOfWork unitOfWork, ICacheService cacheService, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<DeactivateBranchCommand>
{
    public async Task<Result> Handle(DeactivateBranchCommand command, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(command.BranchId, cancellationToken);
        if (branch is null)
            return Result.Failure(BranchErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(command.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        var result = branch.Deactivate();
        if (result.IsFailure)
            return result;

        branchRepository.Update(branch);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        GetBranchesQueryHandler.InvalidateCache(cacheService);

        return Result.Success();
    }
}
