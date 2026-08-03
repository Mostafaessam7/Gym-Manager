using GymManager.Application.Memberships.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Memberships.Subscriptions.PurchaseMembership;

public sealed record PurchaseMembershipCommand(Guid MemberId, Guid MembershipPlanId, DateOnly StartDate)
    : ICommand<Result<MembershipResponse>>;
