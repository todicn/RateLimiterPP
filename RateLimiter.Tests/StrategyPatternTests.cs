using Microsoft.Extensions.Options;
using RateLimiter.Configuration;
using RateLimiter.Implementations;
using RateLimiter.Services;
using Xunit;

namespace RateLimiter.Tests;

public class StrategyPatternTests
{
    private readonly IOptions<RateLimiterOptions> _testOptions;

    public StrategyPatternTests()
    {
        var testOptions = new RateLimiterOptions
        {
            TokenBucket = new TokenBucketOptions { Capacity = 5, RefillRate = 1.0 },
            FixedWindow = new FixedWindowOptions { MaxRequests = 10, WindowSizeInSeconds = 60 },
            SlidingWindow = new SlidingWindowOptions { MaxRequests = 15, WindowSizeInSeconds = 60, SubWindows = 6 }
        };
        _testOptions = Options.Create(testOptions);
    }

    [Fact]
    public async Task TokenBucketStrategy_ShouldAllowBurstThenLimit()
    {
        // Arrange
        var strategy = new TokenBucketStrategy(_testOptions);

        // Act & Assert - Should allow initial burst
        for (int i = 0; i < 5; i++)
        {
            var result = await strategy.TryConsumeAsync();
            Assert.True(result, $"Request {i + 1} should be allowed");
        }

        // Should deny the next request
        var denied = await strategy.TryConsumeAsync();
        Assert.False(denied, "Request after capacity should be denied");
    }

    [Fact]
    public async Task FixedWindowStrategy_ShouldResetAfterWindow()
    {
        // Arrange
        var customOptions = Options.Create(new RateLimiterOptions
        {
            FixedWindow = new FixedWindowOptions { MaxRequests = 2, WindowSizeInSeconds = 1 }
        });
        var strategy = new FixedWindowStrategy(customOptions);

        // Act & Assert - Exhaust the window
        Assert.True(await strategy.TryConsumeAsync());
        Assert.True(await strategy.TryConsumeAsync());
        Assert.False(await strategy.TryConsumeAsync());

        // Wait for window to reset
        await Task.Delay(1100);

        // Should allow requests again
        Assert.True(await strategy.TryConsumeAsync());
    }

    [Fact]
    public async Task SlidingWindowStrategy_ShouldTrackRequestsAcrossSubWindows()
    {
        // Arrange
        var strategy = new SlidingWindowStrategy(_testOptions);

        // Act & Assert - Should allow requests up to limit
        for (int i = 0; i < 10; i++)
        {
            var result = await strategy.TryConsumeAsync();
            Assert.True(result, $"Request {i + 1} should be allowed");
        }

        var available = await strategy.GetAvailablePermitsAsync();
        Assert.Equal(5, available); // 15 max - 10 used = 5 available
    }

    [Fact]
    public void RateLimiterFactory_ShouldCreateCorrectStrategies()
    {
        // Arrange
        var factory = new RateLimiterFactory(_testOptions);

        // Act
        var tokenBucket = factory.CreateTokenBucket();
        var fixedWindow = factory.CreateFixedWindow();
        var slidingWindow = factory.CreateSlidingWindow();

        // Assert
        Assert.IsType<TokenBucketStrategy>(tokenBucket);
        Assert.IsType<FixedWindowStrategy>(fixedWindow);
        Assert.IsType<SlidingWindowStrategy>(slidingWindow);
    }

    [Fact]
    public void RateLimiterFactory_CreateStrategy_ShouldReturnCorrectType()
    {
        // Arrange
        var factory = new RateLimiterFactory(_testOptions);

        // Act & Assert
        var tokenBucket = factory.CreateStrategy(RateLimiterStrategyType.TokenBucket);
        var fixedWindow = factory.CreateStrategy(RateLimiterStrategyType.FixedWindow);
        var slidingWindow = factory.CreateStrategy(RateLimiterStrategyType.SlidingWindow);

        Assert.IsType<TokenBucketStrategy>(tokenBucket);
        Assert.IsType<FixedWindowStrategy>(fixedWindow);
        Assert.IsType<SlidingWindowStrategy>(slidingWindow);
    }

    [Fact]
    public async Task RateLimiterContext_ShouldSwitchStrategies()
    {
        // Arrange
        var factory = new RateLimiterFactory(_testOptions);
        var tokenBucket = factory.CreateTokenBucket();
        var context = new RateLimiterContext(tokenBucket);

        // Act & Assert - Initial strategy
        Assert.Equal("TokenBucketStrategy", context.CurrentStrategyName);
        
        // Switch strategy
        var fixedWindow = factory.CreateFixedWindow();
        context.SetStrategy(fixedWindow);
        
        Assert.Equal("FixedWindowStrategy", context.CurrentStrategyName);
        
        // Verify it delegates correctly
        var result = await context.TryConsumeAsync();
        Assert.True(result);
    }

    [Fact]
    public async Task AllStrategies_ShouldRespectAvailablePermits()
    {
        // Arrange
        var factory = new RateLimiterFactory(_testOptions);
        var strategies = new[]
        {
            factory.CreateTokenBucket(),
            factory.CreateFixedWindow(),
            factory.CreateSlidingWindow()
        };

        // Act & Assert
        foreach (var strategy in strategies)
        {
            var available = await strategy.GetAvailablePermitsAsync();
            Assert.True(available > 0, $"{strategy.GetType().Name} should have available permits");
            
            // Consume one permit
            var consumed = await strategy.TryConsumeAsync();
            Assert.True(consumed, $"{strategy.GetType().Name} should allow consumption");
            
            // Available should decrease
            var newAvailable = await strategy.GetAvailablePermitsAsync();
            Assert.True(newAvailable < available, $"{strategy.GetType().Name} should show decreased availability");
        }
    }
} 