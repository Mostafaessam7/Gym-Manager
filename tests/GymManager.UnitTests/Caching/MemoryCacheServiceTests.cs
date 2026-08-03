using GymManager.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace GymManager.UnitTests.Caching;

public sealed class MemoryCacheServiceTests
{
    private readonly MemoryCacheService _cacheService = new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public async Task GetOrCreateAsync_Should_Invoke_Factory_Only_Once_For_Repeated_Calls()
    {
        var callCount = 0;

        async Task<int> Factory(CancellationToken _)
        {
            callCount++;
            return await Task.FromResult(42);
        }

        var first = await _cacheService.GetOrCreateAsync("key", Factory);
        var second = await _cacheService.GetOrCreateAsync("key", Factory);

        Assert.Equal(42, first);
        Assert.Equal(42, second);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task Remove_Should_Force_The_Next_Call_To_Recreate_The_Value()
    {
        var callCount = 0;
        Task<int> Factory(CancellationToken _) => Task.FromResult(++callCount);

        await _cacheService.GetOrCreateAsync("key", Factory);
        _cacheService.Remove("key");
        await _cacheService.GetOrCreateAsync("key", Factory);

        Assert.Equal(2, callCount);
    }
}
