using Microsoft.Extensions.Options;
using RateLimiter.Configuration;
using RateLimiter.Interfaces;

namespace RateLimiter.Implementations;

/// <summary>
/// Token bucket rate limiter - backward compatibility wrapper.
/// For new code, use TokenBucketStrategy directly or RateLimiterFactory.
/// </summary>
public class TokenBucket : IRateLimiter
{
    private readonly TokenBucketStrategy _strategy;

    public TokenBucket(IOptions<RateLimiterOptions> options)
    {
        _strategy = new TokenBucketStrategy(options);
    }

    public Task<bool> TryConsumeAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        return _strategy.TryConsumeAsync(permits, cancellationToken);
    }

    public Task<int> GetAvailablePermitsAsync(CancellationToken cancellationToken = default)
    {
        return _strategy.GetAvailablePermitsAsync(cancellationToken);
    }

    public Task<TimeSpan> GetWaitTimeAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        return _strategy.GetWaitTimeAsync(permits, cancellationToken);
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        return _strategy.ResetAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the current token count (for testing/debugging purposes)
    /// </summary>
    public double CurrentTokens => _strategy.CurrentTokens;
} 