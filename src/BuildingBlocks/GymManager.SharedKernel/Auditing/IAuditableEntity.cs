namespace GymManager.SharedKernel.Auditing;

/// <summary>Implemented by entities whose creation/modification metadata is tracked automatically.</summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedOnUtc { get; }

    string? CreatedBy { get; }

    DateTimeOffset? ModifiedOnUtc { get; }

    string? ModifiedBy { get; }

    void SetCreated(DateTimeOffset onUtc, string? by);

    void SetModified(DateTimeOffset onUtc, string? by);
}

/// <summary>Implemented by entities that support logical (soft) deletion instead of physical removal.</summary>
public interface ISoftDeletableEntity
{
    bool IsDeleted { get; }

    DateTimeOffset? DeletedOnUtc { get; }

    string? DeletedBy { get; }

    void Delete(DateTimeOffset onUtc, string? by);

    void Restore();
}
