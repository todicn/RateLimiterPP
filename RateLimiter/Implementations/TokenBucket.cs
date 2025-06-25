using RateLimiter.Interfaces;

namespace RateLimiter.Implementations;

/// <summary>
/// Token bucket rate limiter - TODO: Implement this!
/// 
/// The token bucket algorithm allows for bursty traffic while maintaining
/// an average rate limit. Tokens are added to a bucket at a fixed rate.
/// </summary>
public class TokenBucket : IRateLimiter
{
    private readonly int _capacity;
    private readonly double _refillRate;

    public TokenBucket(int capacity, double refillRate)
    {
        _capacity = capacity;
        _refillRate = refillRate;
        // TODO: Initialize your data structures
    }

    public Task<bool> TryConsumeAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        // TODO: Implement token consumption logic
        throw new NotImplementedException();
    }

    public Task<int> GetAvailablePermitsAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Return current available tokens
        throw new NotImplementedException();
    }

    public Task<TimeSpan> GetWaitTimeAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        // TODO: Calculate wait time for permits
        throw new NotImplementedException();
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Reset the bucket state
        throw new NotImplementedException();
    }
} 