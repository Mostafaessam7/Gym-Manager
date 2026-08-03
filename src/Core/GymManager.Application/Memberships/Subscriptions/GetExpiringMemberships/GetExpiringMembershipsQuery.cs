using GymManager.Application.Memberships.Contracts;
using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Memberships.Subscriptions.GetExpiringMemberships;

public sealed record GetExpiringMembershipsQuery(int WithinDays) : IQuery<IReadOnlyList<MembershipResponse>>;
