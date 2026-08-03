using GymManager.Application.Abstractions;
using GymManager.Application.Branches.Contracts;
using GymManager.Application.Branches.GetBranches;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Branches;
using GymManager.Domain.Branches.Errors;
using GymManager.Domain.Common;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Branches.CreateBranch;

public sealed class CreateBranchCommandHandler(IBranchRepository branchRepository, IUnitOfWork unitOfWork, ICacheService cacheService)
    : ICommandHandler<CreateBranchCommand, Result<BranchResponse>>
{
    public async Task<Result<BranchResponse>> Handle(CreateBranchCommand command, CancellationToken cancellationToken)
    {
        if (await branchRepository.NameExistsAsync(command.Name.Trim(), cancellationToken))
            return Result.Failure<BranchResponse>(BranchErrors.NameAlreadyInUse(command.Name));

        var address = Address.Create(command.Country, command.Street, command.City, command.State, command.PostalCode);
        var branch = Branch.Create(command.Name, address, command.PhoneNumber, command.Email);

        branchRepository.Add(branch);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        GetBranchesQueryHandler.InvalidateCache(cacheService);

        return Result.Success(ToResponse(branch));
    }

    internal static BranchResponse ToResponse(Branch branch) => new(
        branch.Id, branch.Name, branch.Address.Street, branch.Address.City, branch.Address.State,
        branch.Address.PostalCode, branch.Address.Country, branch.PhoneNumber, branch.Email, branch.IsActive);
}
