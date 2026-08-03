using GymManager.Application.Memberships.Contracts;
using GymManager.Domain.Memberships;
using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Memberships.Subscriptions.GetMembershipsByMember;

public sealed class GetMembershipsByMemberQueryHandler(IMembershipRepository membershipRepository)
    : IQueryHandler<GetMembershipsByMemberQuery, IReadOnlyList<MembershipResponse>>
{
    public async Task<IReadOnlyList<MembershipResponse>> Handle(GetMembershipsByMemberQuery query, CancellationToken cancellationToken)
    {
        var memberships = await membershipRepository.GetByMemberIdAsync(query.MemberId, cancellationToken);
        return memberships.Select(m => m.ToResponse()).ToList();
    }
}
