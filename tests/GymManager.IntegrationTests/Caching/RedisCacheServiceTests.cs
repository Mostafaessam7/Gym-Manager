using GymManager.Infrastructure.Caching;
using StackExchange.Redis;
using Xunit;

namespace GymManager.IntegrationTests.Caching;

/// <summary>
/// Exercises <see cref="RedisCacheService"/> against a real Redis.
/// </summary>
/// <remarks>
/// These skip themselves when no Redis is reachable, so CI and anyone without one still get a
/// green suite. That is a deliberate trade and worth being honest about: it means the Redis path
/// is only truly covered on a machine that has Redis running. The alternative - a hard dependency
/// - would fail the build for everyone who does not, which is how a test gets deleted.
///
/// The point of covering it at all is that Redis is the only configuration where the cache is
/// shared, and <see cref="RedisCacheService.RemoveByPrefix"/> in particular cannot be exercised
/// any other way: it uses SCAN over the real keyspace, which has no in-memory equivalent. A silent
/// failure there would leave stale plan and trainer lists served across instances.
/// </remarks>
public sealed class RedisCacheServiceTests : IAsyncLifetime
{
    private const string ConnectionString = "localhost:6379,connectTimeout=1000,abortConnect=false";
    private const string KeyPrefix = "gymmanager-test:";

    private IConnectionMultiplexer? _connection;
    private RedisCacheService? _cache;

    private bool Available => _connection is { IsConnected: true };

    public async Task InitializeAsync()
    {
        try
        {
            _connection = await ConnectionMultiplexer.ConnectAsync(ConnectionString);
            _cache = new RedisCacheService(_connection, KeyPrefix);
        }
        catch (RedisConnectionException)
        {
            _connection = null;
        }
    }

    public async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            // Leave no test keys behind - this may be a shared local Redis.
            _cache?.RemoveByPrefix(string.Empty);
            await _connection.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task Values_survive_a_round_trip_and_the_factory_runs_only_on_a_miss()
    {
        Skip.IfNot(Available, "No Redis on localhost:6379.");

        var key = UniqueKey();
        var factoryCalls = 0;

        Task<List<string>> Factory(CancellationToken _)
        {
            factoryCalls++;
            return Task.FromResult(new List<string> { "alpha", "beta" });
        }

        var first = await _cache!.GetOrCreateAsync(key, Factory);
        var second = await _cache.GetOrCreateAsync(key, Factory);

        Assert.Equal(new[] { "alpha", "beta" }, first);
        Assert.Equal(new[] { "alpha", "beta" }, second);

        // The second call must come from Redis. If it did not, the cache is doing nothing at all
        // while still appearing to work.
        Assert.Equal(1, factoryCalls);
    }

    [SkippableFact]
    public async Task Remove_forces_the_next_read_back_to_the_factory()
    {
        Skip.IfNot(Available, "No Redis on localhost:6379.");

        var key = UniqueKey();
        var factoryCalls = 0;

        Task<string> Factory(CancellationToken _)
        {
            factoryCalls++;
            return Task.FromResult($"value-{factoryCalls}");
        }

        await _cache!.GetOrCreateAsync(key, Factory);
        _cache.Remove(key);
        var afterRemoval = await _cache.GetOrCreateAsync(key, Factory);

        Assert.Equal(2, factoryCalls);
        Assert.Equal("value-2", afterRemoval);
    }

    [SkippableFact]
    public async Task RemoveByPrefix_clears_every_matching_key_and_leaves_others_alone()
    {
        Skip.IfNot(Available, "No Redis on localhost:6379.");

        // This is the case that cannot be covered without a real Redis: the in-memory
        // implementation tracks keys in a local dictionary, while this one has to SCAN the
        // keyspace. Plan and trainer caches fan out over branch ids and are invalidated this way,
        // so a failure here means stale lists served across instances with nothing erroring.
        var run = Guid.NewGuid().ToString("N");
        var matching = new[] { $"plans:{run}:branch-1", $"plans:{run}:branch-2", $"plans:{run}:branch-3" };
        var unrelated = $"trainers:{run}:branch-1";

        foreach (var key in matching)
        {
            await _cache!.GetOrCreateAsync(key, _ => Task.FromResult("cached"));
        }

        await _cache!.GetOrCreateAsync(unrelated, _ => Task.FromResult("cached"));

        _cache.RemoveByPrefix($"plans:{run}");

        foreach (var key in matching)
        {
            var calls = 0;
            await _cache.GetOrCreateAsync(key, _ => { calls++; return Task.FromResult("refetched"); });
            Assert.Equal(1, calls);
        }

        // The unrelated key must survive - a prefix removal that clears the whole keyspace would
        // pass the assertions above while quietly destroying every other cache entry.
        var unrelatedCalls = 0;
        await _cache.GetOrCreateAsync(unrelated, _ => { unrelatedCalls++; return Task.FromResult("refetched"); });
        Assert.Equal(0, unrelatedCalls);
    }

    private static string UniqueKey() => $"roundtrip:{Guid.NewGuid():N}";
}
