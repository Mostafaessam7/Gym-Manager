using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Classes;

/// <summary>A class offering catalog entry (e.g. "Morning Yoga"). Bookable occurrences are <see cref="ClassSession"/>s.</summary>
public sealed class GymClass : AggregateRoot<Guid>, IAuditableEntity, ISoftDeletableEntity
{
    private GymClass()
    {
        Name = string.Empty;
        Description = string.Empty;
    }

    private GymClass(Guid id, string name, string description, Guid branchId, Guid trainerId, int capacity, int durationMinutes)
        : base(id)
    {
        Name = name;
        Description = description;
        BranchId = branchId;
        TrainerId = trainerId;
        Capacity = capacity;
        DurationMinutes = durationMinutes;
        IsActive = true;
    }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public Guid BranchId { get; private set; }

    public Guid TrainerId { get; private set; }

    public int Capacity { get; private set; }

    public int DurationMinutes { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedOnUtc { get; private set; }

    public string? DeletedBy { get; private set; }

    public static GymClass Create(string name, string description, Guid branchId, Guid trainerId, int capacity, int durationMinutes) =>
        new(Guid.NewGuid(), name.Trim(), description.Trim(), branchId, trainerId, capacity, durationMinutes);

    public void Update(string name, string description, Guid trainerId, int capacity, int durationMinutes)
    {
        Name = name.Trim();
        Description = description.Trim();
        TrainerId = trainerId;
        Capacity = capacity;
        DurationMinutes = durationMinutes;
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
