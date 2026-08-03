using System.Collections.Concurrent;
using GymManager.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace GymManager.Infrastructure.Caching;

/// <inheritdoc cref="ICacheService"/>
public sealed class MemoryCacheService(IMemoryCache memoryCache) : ICacheService
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    // IMemoryCache has no key-enumeration API, so every key this service creates is tracked here purely to
    // support RemoveByPrefix — the underlying cache entry (and its own expiration) is otherwise unaffected.
    private readonly ConcurrentDictionary<string, byte> _trackedKeys = new();

    public async Task<TValue> GetOrCreateAsync<TValue>(
        string key, Func<CancellationToken, Task<TValue>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (memoryCache.TryGetValue(key, out TValue? cached) && cached is not null)
            return cached;

        var value = await factory(cancellationToken);

        var entryOptions = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration };
        entryOptions.RegisterPostEvictionCallback((evictedKey, _, _, _) => _trackedKeys.TryRemove((string)evictedKey, out _));

        memoryCache.Set(key, value, entryOptions);
        _trackedKeys.TryAdd(key, 0);

        return value;
    }

    public void Remove(string key)
    {
        memoryCache.Remove(key);
        _trackedKeys.TryRemove(key, out _);
    }

    public void RemoveByPrefix(string prefix)
    {
        foreach (var key in _trackedKeys.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)))
            Remove(key);
    }
}
