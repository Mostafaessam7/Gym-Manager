using GymManager.SharedKernel.Primitives;

namespace GymManager.Application.Abstractions;

/// <summary>Publishes domain events raised by aggregates to all registered handlers after a successful commit.</summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
