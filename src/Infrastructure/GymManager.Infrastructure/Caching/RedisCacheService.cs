using System.Text.Json;
using GymManager.Application.Abstractions;
using StackExchange.Redis;

namespace GymManager.Infrastructure.Caching;

/// <inheritdoc cref="ICacheService"/>
/// <remarks>
/// The Redis-backed implementation, used when <c>ConnectionStrings:Redis</c> is configured.
/// <see cref="MemoryCacheService"/> remains the default so local development, CI and the test
/// suite do not need a Redis server.
///
/// Why this exists: <see cref="MemoryCacheService"/> is per-process. On a single instance that is
/// correct and cheaper. On more than one, invalidation stops working across instances - a plan or
/// branch edit clears the cache on the instance that handled the write and leaves every other
/// instance serving its own stale copy until expiry. Nothing errors; the data is just
/// intermittently wrong depending on which instance answers.
///
/// This uses <c>StackExchange.Redis</c> directly rather than <c>IDistributedCache</c> on purpose.
/// <see cref="RemoveByPrefix"/> needs to enumerate keys, and <c>IDistributedCache</c> has no such
/// API - the in-memory implementation works around that by tracking keys in a local dictionary,
/// which would put us straight back to per-process behaviour for exactly the invalidation that
/// matters most (cache keys that fan out over branch ids).
/// </remarks>
public sealed class RedisCacheService(IConnectionMultiplexer connection, string keyPrefix) : ICacheService
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(5);

    private readonly IConnectionMultiplexer _connection = connection;
    private readonly string _keyPrefix = keyPrefix;

    private IDatabase Db => _connection.GetDatabase();

    private string Qualify(string key) => _keyPrefix + key;

    public async Task<TValue> GetOrCreateAsync<TValue>(
        string key, Func<CancellationToken, Task<TValue>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var qualified = Qualify(key);
        var cached = await Db.StringGetAsync(qualified);

        if (cached.HasValue)
        {
            try
            {
                // Explicit cast: RedisValue converts implicitly to both string and
                // ReadOnlySpan<byte>, which makes the Deserialize overload ambiguous.
                var deserialized = JsonSerializer.Deserialize<TValue>((string)cached!);
                if (deserialized is not null)
                    return deserialized;
            }
            catch (JsonException)
            {
                // A payload written by an older response shape is treated as a miss and overwritten
                // below. Redis outlives deployments, so throwing here would mean every read failing
                // until someone flushed the cache by hand.
            }
        }

        var value = await factory(cancellationToken);

        await Db.StringSetAsync(
            qualified,
            JsonSerializer.Serialize(value),
            expiration ?? DefaultExpiration);

        return value;
    }

    public void Remove(string key) => Db.KeyDelete(Qualify(key));

    public void RemoveByPrefix(string prefix)
    {
        var qualified = Qualify(prefix);

        // SCAN rather than KEYS: KEYS blocks the server for the whole scan, which on a shared Redis
        // stalls every other client. `Keys` here issues SCAN under the hood.
        foreach (var endpoint in _connection.GetEndPoints())
        {
            var server = _connection.GetServer(endpoint);

            // A replica would return the same keys the primary already gave us, so skip it rather
            // than delete twice - and writes have to go to a primary anyway.
            if (server.IsReplica || !server.IsConnected)
                continue;

            foreach (var key in server.Keys(pattern: qualified + "*"))
                Db.KeyDelete(key);
        }
    }
}
