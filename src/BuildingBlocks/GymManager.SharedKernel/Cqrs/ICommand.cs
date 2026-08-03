using GymManager.SharedKernel.Results;

namespace GymManager.SharedKernel.Cqrs;

/// <summary>A write operation that mutates state and returns a <see cref="Result"/>.</summary>
public interface ICommand : ICommand<Result>;

/// <summary>A write operation that mutates state and returns <typeparamref name="TResponse"/>.</summary>
public interface ICommand<out TResponse>;

/// <summary>Handles a specific <see cref="ICommand{TResponse}"/>.</summary>
public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken);
}

/// <summary>Handles a specific <see cref="ICommand"/> returning a plain <see cref="Result"/>.</summary>
public interface ICommandHandler<in TCommand> : ICommandHandler<TCommand, Result>
    where TCommand : ICommand;
