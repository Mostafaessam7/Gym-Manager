using GymManager.Application.Abstractions;
using GymManager.SharedKernel.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GymManager.Infrastructure.Events;

/// <inheritdoc cref="IDomainEventDispatcher"/>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
    : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler is null) continue;

                logger.LogDebug(
                    "Dispatching domain event {DomainEvent} to {Handler}",
                    domainEvent.GetType().Name,
                    handler.GetType().Name);

                try
                {
                    var task = (Task)handlerType
                        .GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!
                        .Invoke(handler, [domainEvent, cancellationToken])!;

                    await task;
                }
                catch (Exception exception)
                {
                    // A reactive side effect (email, SMS, in-app notification) failing must never roll back
                    // or fault the business operation that already committed successfully — it's logged and
                    // swallowed instead of propagating up through SaveChangesAsync.
                    logger.LogError(
                        exception,
                        "Domain event handler {Handler} failed while handling {DomainEvent}",
                        handler.GetType().Name,
                        domainEvent.GetType().Name);
                }
            }
        }
    }
}
