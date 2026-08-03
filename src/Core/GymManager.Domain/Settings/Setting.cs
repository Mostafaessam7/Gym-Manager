using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Settings;

/// <summary>A single named, branch-scoped or global (<see cref="BranchId"/> null) configuration value.</summary>
public sealed class Setting : AggregateRoot<Guid>, IAuditableEntity
{
    private Setting()
    {
        Key = string.Empty;
        Value = string.Empty;
    }

    private Setting(Guid id, string key, string value, string? description, Guid? branchId) : base(id)
    {
        Key = key;
        Value = value;
        Description = description;
        BranchId = branchId;
    }

    public string Key { get; private set; }

    public string Value { get; private set; }

    public string? Description { get; private set; }

    /// <summary>Null means the setting applies globally, across every branch.</summary>
    public Guid? BranchId { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public static Setting Create(string key, string value, string? description, Guid? branchId) =>
        new(Guid.NewGuid(), key.Trim(), value, description?.Trim(), branchId);

    public void UpdateValue(string value, string? description)
    {
        Value = value;
        Description = description?.Trim();
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
