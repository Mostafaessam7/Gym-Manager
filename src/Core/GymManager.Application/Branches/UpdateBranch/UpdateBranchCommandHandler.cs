using GymManager.Application.Abstractions;
using GymManager.Application.Branches.GetBranches;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Branches;
using GymManager.Domain.Branches.Errors;
using GymManager.Domain.Common;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Branches.UpdateBranch;

public sealed class UpdateBranchCommandHandler(
    IBranchRepository branchRepository, IUnitOfWork unitOfWork, ICacheService cacheService, IBranchAccessGuard branchAccessGuard)
    : ICommandHandler<UpdateBranchCommand>
{
    public async Task<Result> Handle(UpdateBranchCommand command, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(command.BranchId, cancellationToken);
        if (branch is null)
            return Result.Failure(BranchErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(command.BranchId);
        if (accessResult.IsFailure)
            return accessResult;

        var address = Address.Create(command.Country, command.Street, command.City, command.State, command.PostalCode);
        branch.UpdateDetails(command.Name, address, command.PhoneNumber, command.Email);

        branchRepository.Update(branch);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        GetBranchesQueryHandler.InvalidateCache(cacheService);

        return Result.Success();
    }
}
