using GymManager.SharedKernel.Primitives;

namespace GymManager.Domain.Abstractions;

/// <summary>
/// Generic persistence contract for an aggregate root. Kept minimal — most read scenarios are served
/// by dedicated, dapper-free EF Core query handlers rather than generic repository methods.
/// </summary>
public interface IRepository<TAggregate, in TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    void Add(TAggregate aggregate);

    void Update(TAggregate aggregate);

    void Remove(TAggregate aggregate);
}
