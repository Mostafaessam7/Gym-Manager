namespace GymManager.SharedKernel.Cqrs;

/// <summary>
/// Resolves and invokes the matching <see cref="ICommandHandler{TCommand,TResponse}"/> or
/// <see cref="IQueryHandler{TQuery,TResponse}"/> from the DI container. The in-house replacement
/// for a MediatR sender, kept intentionally minimal for vertical-slice CQRS.
/// </summary>
public interface IDispatcher
{
    Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);

    Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}
