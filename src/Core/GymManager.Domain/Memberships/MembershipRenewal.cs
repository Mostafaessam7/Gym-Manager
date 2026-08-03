using GymManager.Domain.Common;
using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Memberships;

/// <summary>An audit record of a single renewal or initial purchase applied to a <see cref="Membership"/>.</summary>
public sealed class MembershipRenewal : Entity<Guid>
{
    private MembershipRenewal()
    {
        AmountPaid = null!;
    }

    internal MembershipRenewal(DateOnly previousEndDate, DateOnly newEndDate, Money amountPaid) : base(Guid.NewGuid())
    {
        PreviousEndDate = previousEndDate;
        NewEndDate = newEndDate;
        AmountPaid = amountPaid;
        RenewedOnUtc = DateTimeOffset.UtcNow;
    }

    public DateOnly PreviousEndDate { get; private set; }

    public DateOnly NewEndDate { get; private set; }

    public Money AmountPaid { get; private set; }

    public DateTimeOffset RenewedOnUtc { get; private set; }
}
