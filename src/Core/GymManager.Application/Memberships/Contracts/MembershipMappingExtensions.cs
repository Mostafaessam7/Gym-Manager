using GymManager.Domain.Memberships;

namespace GymManager.Application.Memberships.Contracts;

public static class MembershipMappingExtensions
{
    public static MembershipPlanResponse ToResponse(this MembershipPlan plan) => new(
        plan.Id, plan.Name, plan.Description, plan.Price.Amount, plan.Price.Currency,
        plan.DurationInDays, plan.MaxFreezeDays, plan.BranchId, plan.IsActive);

    public static MembershipResponse ToResponse(this Membership membership) => new(
        membership.Id,
        membership.MemberId,
        membership.MembershipPlanId,
        membership.PlanNameSnapshot,
        membership.StartDate,
        membership.EndDate,
        membership.PricePaid.Amount,
        membership.PricePaid.Currency,
        membership.Status.ToString(),
        membership.Renewals
            .Select(r => new MembershipRenewalResponse(r.PreviousEndDate, r.NewEndDate, r.AmountPaid.Amount, r.RenewedOnUtc))
            .ToArray());
}
