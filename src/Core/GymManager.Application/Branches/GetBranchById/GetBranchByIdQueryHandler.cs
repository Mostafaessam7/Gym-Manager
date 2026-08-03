using GymManager.Application.Abstractions;
using GymManager.Application.Branches.Contracts;
using GymManager.Domain.Branches.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Branches.GetBranchById;

public sealed class GetBranchByIdQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard) : IQueryHandler<GetBranchByIdQuery, Result<BranchResponse>>
{
    public async Task<Result<BranchResponse>> Handle(GetBranchByIdQuery query, CancellationToken cancellationToken)
    {
        var branch = await readDb.Branches.FirstOrDefaultAsync(b => b.Id == query.BranchId, cancellationToken);
        if (branch is null)
            return Result.Failure<BranchResponse>(BranchErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(branch.Id);
        if (accessResult.IsFailure)
            return Result.Failure<BranchResponse>(accessResult.Error);

        return Result.Success(new BranchResponse(
            branch.Id, branch.Name, branch.Address.Street, branch.Address.City, branch.Address.State,
            branch.Address.PostalCode, branch.Address.Country, branch.PhoneNumber, branch.Email, branch.IsActive));
    }
}
