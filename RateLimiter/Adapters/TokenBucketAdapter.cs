using Microsoft.Extensions.Options;
using RateLimiter.Configuration;
using RateLimiter.Implementations;
using RateLimiter.Interfaces;

namespace RateLimiter.Adapters;

/// <summary>
/// Adapter that bridges the legacy TokenBucket interface with the new TokenBucketStrategy implementation.
/// This ensures backward compatibility while allowing access to new features.
/// </summary>
public class TokenBucketAdapter : IRateLimiterAdapter
{
    private IRateLimiter _strategy;
    private readonly TokenBucketStrategy _tokenBucketStrategy;

    public TokenBucketAdapter(IOptions<RateLimiterOptions> options)
    {
        _tokenBucketStrategy = new TokenBucketStrategy(options);
        _strategy = _tokenBucketStrategy;
    }

    public TokenBucketAdapter(int capacity, double refillRate)
    {
        var options = Options.Create(new RateLimiterOptions
        {
            TokenBucket = new TokenBucketOptions
            {
                Capacity = capacity,
                RefillRate = refillRate
            }
        });
        _tokenBucketStrategy = new TokenBucketStrategy(options);
        _strategy = _tokenBucketStrategy;
    }

    public string StrategyTypeName => _strategy.GetType().Name;

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

    public void SetStrategy(IRateLimiter strategy)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    public object GetDebugInfo()
    {
        if (_strategy is TokenBucketStrategy tbs)
        {
            return new
            {
                CurrentTokens = tbs.CurrentTokens,
                StrategyType = "TokenBucket"
            };
        }

        return new
        {
            StrategyType = _strategy.GetType().Name
        };
    }

    /// <summary>
    /// Legacy property access for backward compatibility
    /// </summary>
    public double CurrentTokens => 
        _strategy is TokenBucketStrategy tbs ? tbs.CurrentTokens : 0;
} 