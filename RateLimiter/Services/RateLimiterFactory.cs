using Microsoft.Extensions.Options;
using RateLimiter.Configuration;
using RateLimiter.Implementations;
using RateLimiter.Interfaces;

namespace RateLimiter.Services;

/// <summary>
/// Factory for creating different rate limiting strategies.
/// </summary>
public class RateLimiterFactory
{
    private readonly IOptions<RateLimiterOptions> _options;

    public RateLimiterFactory(IOptions<RateLimiterOptions> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Creates a token bucket rate limiter strategy.
    /// </summary>
    public IRateLimiter CreateTokenBucket()
    {
        return new TokenBucketStrategy(_options);
    }

    /// <summary>
    /// Creates a fixed window rate limiter strategy.
    /// </summary>
    public IRateLimiter CreateFixedWindow()
    {
        return new FixedWindowStrategy(_options);
    }

    /// <summary>
    /// Creates a sliding window rate limiter strategy.
    /// </summary>
    public IRateLimiter CreateSlidingWindow()
    {
        return new SlidingWindowStrategy(_options);
    }

    /// <summary>
    /// Creates a rate limiter strategy based on the specified type.
    /// </summary>
    /// <param name="strategyType">The type of strategy to create</param>
    public IRateLimiter CreateStrategy(RateLimiterStrategyType strategyType)
    {
        return strategyType switch
        {
            RateLimiterStrategyType.TokenBucket => CreateTokenBucket(),
            RateLimiterStrategyType.FixedWindow => CreateFixedWindow(),
            RateLimiterStrategyType.SlidingWindow => CreateSlidingWindow(),
            _ => throw new ArgumentException($"Unknown strategy type: {strategyType}", nameof(strategyType))
        };
    }
}

/// <summary>
/// Enumeration of available rate limiting strategy types.
/// </summary>
public enum RateLimiterStrategyType
{
    TokenBucket,
    FixedWindow,
    SlidingWindow
} 