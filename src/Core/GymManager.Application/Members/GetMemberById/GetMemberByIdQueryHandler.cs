using GymManager.Application.Abstractions;
using GymManager.Application.Members.Contracts;
using GymManager.Domain.Members.Errors;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Members.GetMemberById;

public sealed class GetMemberByIdQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard) : IQueryHandler<GetMemberByIdQuery, Result<MemberResponse>>
{
    public async Task<Result<MemberResponse>> Handle(GetMemberByIdQuery query, CancellationToken cancellationToken)
    {
        var member = await readDb.Members.FirstOrDefaultAsync(m => m.Id == query.MemberId, cancellationToken);
        if (member is null)
            return Result.Failure<MemberResponse>(MemberErrors.NotFound);

        var accessResult = branchAccessGuard.EnsureCanAccess(member.BranchId);
        if (accessResult.IsFailure)
            return Result.Failure<MemberResponse>(accessResult.Error);

        return Result.Success(member.ToResponse());
    }
}
