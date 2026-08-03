namespace GymManager.SharedKernel.Primitives;

/// <summary>
/// Base class for all domain entities. Identity-based equality.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity(TId id)
    {
        Id = id;
    }

    protected Entity()
    {
    }

    public TId Id { get; protected set; } = default!;

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => obj is Entity<TId> entity && Equals(entity);

    public override int GetHashCode() => (GetType().ToString() + Id).GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null && right is null || (left is not null && right is not null && left.Equals(right));

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
