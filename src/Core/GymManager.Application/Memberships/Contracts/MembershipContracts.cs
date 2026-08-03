namespace GymManager.Application.Memberships.Contracts;

public sealed record MembershipPlanResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int DurationInDays,
    int MaxFreezeDays,
    Guid? BranchId,
    bool IsActive);

public sealed record MembershipRenewalResponse(DateOnly PreviousEndDate, DateOnly NewEndDate, decimal AmountPaid, DateTimeOffset RenewedOnUtc);

public sealed record MembershipResponse(
    Guid Id,
    Guid MemberId,
    Guid MembershipPlanId,
    string PlanNameSnapshot,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal PricePaid,
    string Currency,
    string Status,
    IReadOnlyCollection<MembershipRenewalResponse> Renewals);
