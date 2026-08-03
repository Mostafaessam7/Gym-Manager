namespace GymManager.SharedKernel.Primitives;

/// <summary>
/// Marks an entity as the root of an aggregate — the only entry point for modifications
/// within its consistency boundary.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    protected AggregateRoot(TId id) : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    /// <summary>Concurrency token mapped to a SQL Server rowversion column.</summary>
    public byte[] RowVersion { get; protected set; } = [];
}
