using RateLimiter.Interfaces;

namespace RateLimiter.Services;

/// <summary>
/// Rate limiter context that uses the strategy pattern to switch between different rate limiting algorithms.
/// This allows for runtime strategy selection and testing different algorithms.
/// </summary>
public class RateLimiterContext : IRateLimiter
{
    private IRateLimiter _strategy;

    /// <summary>
    /// Initializes the context with a default strategy.
    /// </summary>
    /// <param name="strategy">The initial rate limiting strategy to use</param>
    public RateLimiterContext(IRateLimiter strategy)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    /// <summary>
    /// Changes the rate limiting strategy at runtime.
    /// </summary>
    /// <param name="strategy">The new strategy to use</param>
    public void SetStrategy(IRateLimiter strategy)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    /// <summary>
    /// Gets the current strategy type name for debugging/logging purposes.
    /// </summary>
    public string CurrentStrategyName => _strategy.GetType().Name;

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
} 