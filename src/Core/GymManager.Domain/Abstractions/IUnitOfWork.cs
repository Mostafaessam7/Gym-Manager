namespace GymManager.Domain.Abstractions;

/// <summary>Commits all changes tracked across the repositories participating in a single business transaction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
