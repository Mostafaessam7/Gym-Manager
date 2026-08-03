namespace GymManager.SharedKernel.Primitives;

/// <summary>
/// Non-generic marker allowing infrastructure code (e.g. the EF Core <c>DbContext</c>) to collect domain
/// events across every tracked <see cref="Entity{TId}"/> regardless of its identifier type.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
