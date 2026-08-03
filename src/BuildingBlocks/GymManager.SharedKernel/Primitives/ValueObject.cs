namespace GymManager.SharedKernel.Primitives;

/// <summary>
/// Base class for immutable value objects compared by their component values rather than identity.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj) => obj is ValueObject valueObject && Equals(valueObject);

    public override int GetHashCode() =>
        GetEqualityComponents()
            .Aggregate(17, (current, obj) => current * 31 + (obj?.GetHashCode() ?? 0));

    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left is null && right is null || (left is not null && right is not null && left.Equals(right));

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
