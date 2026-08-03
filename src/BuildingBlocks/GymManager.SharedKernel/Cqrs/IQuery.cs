namespace GymManager.SharedKernel.Cqrs;

/// <summary>A side-effect-free read operation that returns <typeparamref name="TResponse"/>.</summary>
public interface IQuery<out TResponse>;

/// <summary>Handles a specific <see cref="IQuery{TResponse}"/>.</summary>
public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken);
}
