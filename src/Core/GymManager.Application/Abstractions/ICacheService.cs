namespace GymManager.Application.Abstractions;

/// <summary>A thin, provider-agnostic seam over the process cache used by read-heavy, rarely-changing queries.</summary>
public interface ICacheService
{
    Task<TValue> GetOrCreateAsync<TValue>(
        string key, Func<CancellationToken, Task<TValue>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    void Remove(string key);

    /// <summary>Removes every cached entry whose key starts with <paramref name="prefix"/>, for cache keys
    /// that fan out over an unbounded parameter (e.g. a branch id) where every exact key can't be enumerated
    /// up front the way <see cref="Remove"/> requires.</summary>
    void RemoveByPrefix(string prefix);
}
