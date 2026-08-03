using GymManager.Application.Abstractions;
using GymManager.Application.Memberships.Contracts;
using GymManager.Domain.Memberships;
using GymManager.SharedKernel.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Memberships.Subscriptions.GetExpiringMemberships;

public sealed class GetExpiringMembershipsQueryHandler(
    IMembershipRepository membershipRepository, IDateTimeProvider dateTimeProvider,
    IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetExpiringMembershipsQuery, IReadOnlyList<MembershipResponse>>
{
    public async Task<IReadOnlyList<MembershipResponse>> Handle(GetExpiringMembershipsQuery query, CancellationToken cancellationToken)
    {
        var today = dateTimeProvider.TodayUtc;
        var memberships = await membershipRepository.GetActiveMembershipsExpiringBetweenAsync(
            today, today.AddDays(query.WithinDays), cancellationToken);

        var branchId = branchAccessGuard.ResolveFilter(null);
        if (branchId.HasValue)
        {
            var branchMemberIds = await readDb.Members.Where(m => m.BranchId == branchId).Select(m => m.Id).ToListAsync(cancellationToken);
            memberships = memberships.Where(m => branchMemberIds.Contains(m.MemberId)).ToList();
        }

        return memberships.Select(m => m.ToResponse()).ToList();
    }
}
