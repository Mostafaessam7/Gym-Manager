using GymManager.Application.Abstractions;
using GymManager.Application.Identity.Contracts;
using GymManager.Domain.Identity.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Identity.Users.GetUserById;

public sealed class GetUserByIdQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard) : IQueryHandler<GetUserByIdQuery, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        var user = await readDb.Users.FirstOrDefaultAsync(u => u.Id == query.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<UserResponse>(UserErrors.NotFound);

        if (user.BranchId.HasValue)
        {
            var accessResult = branchAccessGuard.EnsureCanAccess(user.BranchId.Value);
            if (accessResult.IsFailure)
                return Result.Failure<UserResponse>(accessResult.Error);
        }

        var roleIds = user.Roles.Select(r => r.RoleId).ToArray();
        var roleNames = await readDb.Roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Name).ToArrayAsync(cancellationToken);

        return Result.Success(new UserResponse(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.IsActive,
            user.BranchId,
            roleNames,
            user.CreatedOnUtc));
    }
}
