namespace GymManager.SharedKernel.Primitives;

/// <summary>
/// Marker interface for domain events raised by aggregates and dispatched after persistence.
/// </summary>
public interface IDomainEvent
{
    Guid EventId => Guid.NewGuid();

    DateTimeOffset OccurredOnUtc => DateTimeOffset.UtcNow;
}

/// <summary>
/// Handles a single <see cref="IDomainEvent"/> type. Implementations are resolved from the
/// DI container and invoked by the in-process <c>IDomainEventDispatcher</c>.
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
