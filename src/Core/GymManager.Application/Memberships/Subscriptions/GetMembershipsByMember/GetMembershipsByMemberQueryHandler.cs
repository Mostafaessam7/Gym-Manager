using GymManager.Application.Abstractions;
using GymManager.Application.Memberships.Contracts;
using GymManager.Domain.Memberships;
using GymManager.SharedKernel.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Memberships.Subscriptions.GetMembershipsByMember;

public sealed class GetMembershipsByMemberQueryHandler(
    IMembershipRepository membershipRepository, IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetMembershipsByMemberQuery, IReadOnlyList<MembershipResponse>>
{
    public async Task<IReadOnlyList<MembershipResponse>> Handle(GetMembershipsByMemberQuery query, CancellationToken cancellationToken)
    {
        // Membership has no BranchId of its own — it's scoped through the member's own branch. A
        // branch-scoped caller who names a member outside their branch sees an empty list rather than a
        // 403, matching how every other member-scoped list query in this codebase (Nutrition, Workouts,
        // Body Measurements) already behaves for consistency.
        var member = await readDb.Members.FirstOrDefaultAsync(m => m.Id == query.MemberId, cancellationToken);
        if (member is not null && branchAccessGuard.EnsureCanAccess(member.BranchId).IsFailure)
            return [];

        var memberships = await membershipRepository.GetByMemberIdAsync(query.MemberId, cancellationToken);
        return memberships.Select(m => m.ToResponse()).ToList();
    }
}
