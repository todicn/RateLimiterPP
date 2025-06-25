using Microsoft.Extensions.Options;
using RateLimiter.Configuration;
using RateLimiter.Interfaces;
using System.Diagnostics;

namespace RateLimiter.Implementations;

/// <summary>
/// Token bucket rate limiter strategy implementation.
/// 
/// The token bucket algorithm allows for bursty traffic while maintaining
/// an average rate limit. Tokens are added to a bucket at a fixed rate.
/// </summary>
public class TokenBucketStrategy : IRateLimiter
{
    private readonly TokenBucketOptions _options;
    private readonly object _lock = new();
    
    private double _tokens;
    private long _lastRefillTimestamp;

    public TokenBucketStrategy(IOptions<RateLimiterOptions> options)
    {
        _options = options.Value.TokenBucket;
        _tokens = _options.Capacity; // Start with full bucket
        _lastRefillTimestamp = Stopwatch.GetTimestamp();
    }

    public Task<bool> TryConsumeAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        if (permits <= 0)
            throw new ArgumentException("Permits must be positive", nameof(permits));

        lock (_lock)
        {
            Refill();
            
            if (_tokens >= permits)
            {
                _tokens -= permits;
                return Task.FromResult(true);
            }
            
            return Task.FromResult(false);
        }
    }

    public Task<int> GetAvailablePermitsAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            Refill();
            return Task.FromResult((int)Math.Floor(_tokens));
        }
    }

    public Task<TimeSpan> GetWaitTimeAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        if (permits <= 0)
            throw new ArgumentException("Permits must be positive", nameof(permits));

        lock (_lock)
        {
            Refill();
            
            if (_tokens >= permits)
                return Task.FromResult(TimeSpan.Zero);
            
            var tokensNeeded = permits - _tokens;
            var waitTimeSeconds = tokensNeeded / _options.RefillRate;
            return Task.FromResult(TimeSpan.FromSeconds(waitTimeSeconds));
        }
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _tokens = _options.Capacity;
            _lastRefillTimestamp = Stopwatch.GetTimestamp();
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Refills the bucket based on elapsed time since last refill.
    /// Must be called while holding the lock.
    /// </summary>
    private void Refill()
    {
        var now = Stopwatch.GetTimestamp();
        var elapsedSeconds = (now - _lastRefillTimestamp) / (double)Stopwatch.Frequency;
        
        if (elapsedSeconds > 0)
        {
            var tokensToAdd = elapsedSeconds * _options.RefillRate;
            _tokens = Math.Min(_options.Capacity, _tokens + tokensToAdd);
            _lastRefillTimestamp = now;
        }
    }

    /// <summary>
    /// Gets the current token count (for testing/debugging purposes)
    /// </summary>
    public double CurrentTokens
    {
        get
        {
            lock (_lock)
            {
                Refill();
                return _tokens;
            }
        }
    }
} 