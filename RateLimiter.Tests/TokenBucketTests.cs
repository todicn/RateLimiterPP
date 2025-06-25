using Microsoft.Extensions.Options;
using RateLimiter.Configuration;
using RateLimiter.Implementations;
using Xunit;

namespace RateLimiter.Tests;

public class TokenBucketTests
{
    private readonly IOptions<RateLimiterOptions> _testOptions;

    public TokenBucketTests()
    {
        // Setup test configuration
        var testOptions = new RateLimiterOptions
        {
            TokenBucket = new TokenBucketOptions
            {
                Capacity = 10,
                RefillRate = 1.0
            }
        };
        _testOptions = Options.Create(testOptions);
    }

    [Fact]
    public async Task TryConsumeAsync_WithAvailableTokens_ShouldReturnTrue()
    {
        // Arrange
        var bucket = new TokenBucket(_testOptions);

        // Act & Assert
        // TODO: Write your test here
        // bucket should start with full capacity (10 tokens)
        Assert.True(false, "Implement this test!");
    }

    [Fact]
    public async Task TryConsumeAsync_WithoutAvailableTokens_ShouldReturnFalse()
    {
        // Arrange
        var bucket = new TokenBucket(_testOptions);

        // TODO: Write test for when tokens are exhausted
        // Consume all tokens first, then verify rejection
        Assert.True(false, "Implement this test!");
    }

    [Fact]
    public async Task TokensRefill_OverTime_ShouldRestoreCapacity()
    {
        // Arrange
        var bucket = new TokenBucket(_testOptions);

        // TODO: Write test for token refill behavior
        // Test that tokens refill at the configured rate (1.0 per second)
        Assert.True(false, "Implement this test!");
    }

    [Fact]
    public async Task Configuration_ShouldBeRespected()
    {
        // Arrange - Test with different configuration
        var customOptions = Options.Create(new RateLimiterOptions
        {
            TokenBucket = new TokenBucketOptions
            {
                Capacity = 5,
                RefillRate = 2.0
            }
        });
        var bucket = new TokenBucket(customOptions);

        // TODO: Verify that the bucket respects the injected configuration
        Assert.True(false, "Implement this test!");
    }
} 