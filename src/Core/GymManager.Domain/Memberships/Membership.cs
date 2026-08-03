using GymManager.Domain.Common;
using GymManager.Domain.Memberships.Errors;
using GymManager.Domain.Memberships.Events;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;
using GymManager.SharedKernel.Results;

namespace GymManager.Domain.Memberships;

/// <summary>A member's purchased subscription to a <see cref="MembershipPlan"/>, including its full renewal history.</summary>
public sealed class Membership : AggregateRoot<Guid>, IAuditableEntity
{
    private readonly List<MembershipRenewal> _renewals = [];

    private Membership()
    {
        PlanNameSnapshot = string.Empty;
        PricePaid = null!;
    }

    private Membership(
        Guid id, Guid memberId, Guid membershipPlanId, string planNameSnapshot, DateOnly startDate, DateOnly endDate, Money pricePaid)
        : base(id)
    {
        MemberId = memberId;
        MembershipPlanId = membershipPlanId;
        PlanNameSnapshot = planNameSnapshot;
        StartDate = startDate;
        EndDate = endDate;
        PricePaid = pricePaid;
        Status = MembershipStatus.Active;
    }

    public Guid MemberId { get; private set; }

    public Guid MembershipPlanId { get; private set; }

    public string PlanNameSnapshot { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public Money PricePaid { get; private set; }

    public MembershipStatus Status { get; private set; }

    public DateTimeOffset? FrozenOnUtc { get; private set; }

    public IReadOnlyCollection<MembershipRenewal> Renewals => _renewals.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsCurrentlyActive(DateOnly today) => Status == MembershipStatus.Active && EndDate >= today;

    public static Membership Purchase(Guid memberId, Guid membershipPlanId, string planNameSnapshot, DateOnly startDate, int durationInDays, Money pricePaid)
    {
        var membership = new Membership(
            Guid.NewGuid(), memberId, membershipPlanId, planNameSnapshot, startDate, startDate.AddDays(durationInDays), pricePaid);

        membership.Raise(new MembershipPurchasedDomainEvent(membership.Id, memberId, membershipPlanId));
        return membership;
    }

    public Result Renew(int additionalDays, Money amountPaid, DateOnly today)
    {
        if (Status == MembershipStatus.Cancelled)
            return Result.Failure(MembershipErrors.CannotRenewCancelled);

        var previousEndDate = EndDate;
        var renewalBaseDate = EndDate < today ? today : EndDate;
        EndDate = renewalBaseDate.AddDays(additionalDays);

        _renewals.Add(new MembershipRenewal(previousEndDate, EndDate, amountPaid));
        Status = MembershipStatus.Active;
        FrozenOnUtc = null;

        Raise(new MembershipRenewedDomainEvent(Id, MemberId, EndDate));
        return Result.Success();
    }

    public Result Freeze()
    {
        if (Status != MembershipStatus.Active)
            return Result.Failure(MembershipErrors.NotActive);

        Status = MembershipStatus.Frozen;
        FrozenOnUtc = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public Result Unfreeze()
    {
        if (Status != MembershipStatus.Frozen || FrozenOnUtc is null)
            return Result.Failure(MembershipErrors.NotFrozen);

        var frozenDays = (int)Math.Ceiling((DateTimeOffset.UtcNow - FrozenOnUtc.Value).TotalDays);
        EndDate = EndDate.AddDays(Math.Max(frozenDays, 0));

        Status = MembershipStatus.Active;
        FrozenOnUtc = null;
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status == MembershipStatus.Cancelled)
            return Result.Failure(MembershipErrors.AlreadyCancelled);

        Status = MembershipStatus.Cancelled;
        Raise(new MembershipCancelledDomainEvent(Id, MemberId));
        return Result.Success();
    }

    public void MarkExpired(DateOnly today)
    {
        if (Status != MembershipStatus.Active || EndDate >= today)
            return;

        Status = MembershipStatus.Expired;
        Raise(new MembershipExpiredDomainEvent(Id, MemberId));
    }

    public void SetCreated(DateTimeOffset onUtc, string? by)
    {
        CreatedOnUtc = onUtc;
        CreatedBy = by;
    }

    public void SetModified(DateTimeOffset onUtc, string? by)
    {
        ModifiedOnUtc = onUtc;
        ModifiedBy = by;
    }
}
