using GymManager.Domain.Common;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Memberships;

/// <summary>A purchasable membership offering (e.g. "Monthly", "Annual Gold"). <see cref="Membership"/> instances
/// snapshot the plan's name and price at purchase time so later plan edits never rewrite billing history.</summary>
public sealed class MembershipPlan : AggregateRoot<Guid>, IAuditableEntity, ISoftDeletableEntity
{
    private MembershipPlan()
    {
        Name = string.Empty;
        Description = string.Empty;
        Price = null!;
    }

    private MembershipPlan(Guid id, string name, string description, Money price, int durationInDays, int maxFreezeDays, Guid? branchId)
        : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        DurationInDays = durationInDays;
        MaxFreezeDays = maxFreezeDays;
        BranchId = branchId;
        IsActive = true;
    }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public Money Price { get; private set; }

    public int DurationInDays { get; private set; }

    public int MaxFreezeDays { get; private set; }

    /// <summary>Null means the plan is offered at every branch.</summary>
    public Guid? BranchId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedOnUtc { get; private set; }

    public string? DeletedBy { get; private set; }

    public static MembershipPlan Create(string name, string description, Money price, int durationInDays, int maxFreezeDays, Guid? branchId) =>
        new(Guid.NewGuid(), name.Trim(), description.Trim(), price, durationInDays, maxFreezeDays, branchId);

    public void Update(string name, string description, Money price, int durationInDays, int maxFreezeDays)
    {
        Name = name.Trim();
        Description = description.Trim();
        Price = price;
        DurationInDays = durationInDays;
        MaxFreezeDays = maxFreezeDays;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

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

    public void Delete(DateTimeOffset onUtc, string? by)
    {
        IsDeleted = true;
        DeletedOnUtc = onUtc;
        DeletedBy = by;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
    }
}
