using Microsoft.Extensions.Options;
using RateLimiter.Adapters;
using RateLimiter.Configuration;
using RateLimiter.Implementations;
using Xunit;

namespace RateLimiter.Tests;

public class AdapterTests
{
    private readonly IOptions<RateLimiterOptions> _testOptions;

    public AdapterTests()
    {
        var testOptions = new RateLimiterOptions
        {
            TokenBucket = new TokenBucketOptions { Capacity = 10, RefillRate = 1.0 },
            FixedWindow = new FixedWindowOptions { MaxRequests = 15, WindowSizeInSeconds = 60 },
            SlidingWindow = new SlidingWindowOptions { MaxRequests = 20, WindowSizeInSeconds = 60, SubWindows = 6 }
        };
        _testOptions = Options.Create(testOptions);
    }

    [Fact]
    public async Task TokenBucketAdapter_WithConstructorParams_ShouldWork()
    {
        // Arrange
        var adapter = new TokenBucketAdapter(capacity: 5, refillRate: 2.0);

        // Act
        var result1 = await adapter.TryConsumeAsync();
        var result2 = await adapter.TryConsumeAsync(3);
        var available = await adapter.GetAvailablePermitsAsync();

        // Assert
        Assert.True(result1, "First request should be allowed");
        Assert.True(result2, "Second request for 3 permits should be allowed");
        Assert.Equal(1, available); // 5 - 1 - 3 = 1
        Assert.Equal("TokenBucketStrategy", adapter.StrategyTypeName);
    }

    [Fact]
    public async Task TokenBucketAdapter_WithOptions_ShouldWork()
    {
        // Arrange
        var adapter = new TokenBucketAdapter(_testOptions);

        // Act
        var initialAvailable = await adapter.GetAvailablePermitsAsync();
        var result = await adapter.TryConsumeAsync(5);
        var afterAvailable = await adapter.GetAvailablePermitsAsync();

        // Assert
        Assert.Equal(10, initialAvailable);
        Assert.True(result, "Request for 5 permits should be allowed");
        Assert.Equal(5, afterAvailable);
    }

    [Fact]
    public async Task UniversalAdapter_ShouldSwitchStrategies()
    {
        // Arrange
        var tokenBucket = new TokenBucketStrategy(_testOptions);
        var adapter = new UniversalRateLimiterAdapter(tokenBucket);

        // Act & Assert - Initial strategy
        Assert.Equal("TokenBucketStrategy", adapter.StrategyTypeName);
        var result1 = await adapter.TryConsumeAsync();
        Assert.True(result1);

        // Switch to fixed window
        var fixedWindow = new FixedWindowStrategy(_testOptions);
        adapter.SetStrategy(fixedWindow);

        Assert.Equal("FixedWindowStrategy", adapter.StrategyTypeName);
        var result2 = await adapter.TryConsumeAsync();
        Assert.True(result2);
    }

    [Fact]
    public async Task TokenBucketAdapter_ShouldProvideDebugInfo()
    {
        // Arrange
        var adapter = new TokenBucketAdapter(capacity: 8, refillRate: 1.0);

        // Act
        await adapter.TryConsumeAsync(3);
        var debugInfo = adapter.GetDebugInfo();

        // Assert
        Assert.NotNull(debugInfo);
        Assert.Equal("TokenBucketStrategy", adapter.StrategyTypeName);
        
        // Should have current tokens info
        Assert.True(adapter.CurrentTokens > 0);
        Assert.True(adapter.CurrentTokens <= 8);
    }

    [Fact]
    public async Task UniversalAdapter_ShouldProvideDebugInfo()
    {
        // Arrange
        var strategy = new TokenBucketStrategy(_testOptions);
        var adapter = new UniversalRateLimiterAdapter(strategy);

        // Act
        var debugInfo = adapter.GetDebugInfo();

        // Assert
        Assert.NotNull(debugInfo);
        Assert.Equal("TokenBucketStrategy", adapter.StrategyTypeName);
        
        // Verify adapter can provide debug info
        Assert.NotNull(debugInfo);
    }

    [Fact]
    public async Task TokenBucketAdapter_ShouldHandleResetCorrectly()
    {
        // Arrange
        var adapter = new TokenBucketAdapter(capacity: 5, refillRate: 1.0);

        // Act - Consume all tokens
        for (int i = 0; i < 5; i++)
        {
            await adapter.TryConsumeAsync();
        }

        var beforeReset = await adapter.GetAvailablePermitsAsync();
        await adapter.ResetAsync();
        var afterReset = await adapter.GetAvailablePermitsAsync();

        // Assert
        Assert.Equal(0, beforeReset);
        Assert.Equal(5, afterReset);
    }

    [Fact]
    public async Task TokenBucketAdapter_ShouldCalculateWaitTimeCorrectly()
    {
        // Arrange
        var adapter = new TokenBucketAdapter(capacity: 3, refillRate: 1.0);

        // Act - Exhaust tokens
        await adapter.TryConsumeAsync(3);
        var waitTime = await adapter.GetWaitTimeAsync(2);

        // Assert
        Assert.True(waitTime > TimeSpan.Zero, "Should need to wait for tokens");
        Assert.True(waitTime.TotalSeconds > 0, "Wait time should be positive");
    }
} 