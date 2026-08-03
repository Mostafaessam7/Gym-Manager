using System.Collections.Concurrent;
using FluentValidation;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;
using Microsoft.Extensions.DependencyInjection;

namespace GymManager.Application.Cqrs;

/// <summary>
/// Reflection-based, DI-driven dispatcher that resolves the matching command/query handler and,
/// for commands, runs any registered <see cref="IValidator{T}"/> beforehand. This is the sole
/// in-house replacement for a MediatR sender used throughout the vertical slices.
/// </summary>
public sealed class Dispatcher(IServiceProvider serviceProvider) : IDispatcher
{
    private static readonly ConcurrentDictionary<Type, Type> HandlerTypeCache = new();

    public async Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        var commandType = command.GetType();

        var validatorType = typeof(IValidator<>).MakeGenericType(commandType);
        if (serviceProvider.GetService(validatorType) is IValidator validator)
        {
            var validationContext = new ValidationContext<object>(command);
            var validationResult = await validator.ValidateAsync(validationContext, cancellationToken);

            if (!validationResult.IsValid)
            {
                var error = Error.Validation(
                    "Validation.Failed",
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

                if (typeof(TResponse) == typeof(Result))
                    return (TResponse)(object)Result.Failure(error);

                if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var failureMethod = typeof(Result)
                        .GetMethod(nameof(Result.Failure), 1, [typeof(Error)])!
                        .MakeGenericMethod(typeof(TResponse).GetGenericArguments()[0]);
                    return (TResponse)failureMethod.Invoke(null, [error])!;
                }

                throw new ValidationException(validationResult.Errors);
            }
        }

        var handlerType = HandlerTypeCache.GetOrAdd(
            commandType,
            t => typeof(ICommandHandler<,>).MakeGenericType(t, typeof(TResponse)));

        dynamic handler = serviceProvider.GetRequiredService(handlerType);
        return await handler.Handle((dynamic)command, cancellationToken);
    }

    public async Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        var queryType = query.GetType();

        var handlerType = HandlerTypeCache.GetOrAdd(
            queryType,
            t => typeof(IQueryHandler<,>).MakeGenericType(t, typeof(TResponse)));

        dynamic handler = serviceProvider.GetRequiredService(handlerType);
        return await handler.Handle((dynamic)query, cancellationToken);
    }
}
