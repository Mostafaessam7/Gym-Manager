using GymManager.Application.Memberships.Contracts;
using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Memberships.Subscriptions.GetMembershipsByMember;

public sealed record GetMembershipsByMemberQuery(Guid MemberId) : IQuery<IReadOnlyList<MembershipResponse>>;
