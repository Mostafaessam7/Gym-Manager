using GymManager.Application.Abstractions;
using GymManager.Domain.Branches.Errors;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Services;

/// <inheritdoc cref="IBranchAccessGuard"/>
public sealed class BranchAccessGuard(ICurrentUserService currentUserService) : IBranchAccessGuard
{
    public Result EnsureCanAccess(Guid branchId)
    {
        var callerBranchId = currentUserService.BranchId;

        return callerBranchId.HasValue && callerBranchId.Value != branchId
            ? Result.Failure(BranchErrors.AccessDenied)
            : Result.Success();
    }

    public Guid? ResolveFilter(Guid? requestedBranchId) => currentUserService.BranchId ?? requestedBranchId;
}
