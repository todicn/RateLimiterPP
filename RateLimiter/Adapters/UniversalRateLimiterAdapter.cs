using RateLimiter.Interfaces;
using RateLimiter.Services;

namespace RateLimiter.Adapters;

/// <summary>
/// Universal adapter that can work with any rate limiting strategy.
/// This provides a consistent interface for testing and legacy code integration.
/// </summary>
public class UniversalRateLimiterAdapter : IRateLimiterAdapter
{
    private readonly RateLimiterContext _context;

    public UniversalRateLimiterAdapter(IRateLimiter initialStrategy)
    {
        _context = new RateLimiterContext(initialStrategy);
    }

    public string StrategyTypeName => _context.CurrentStrategyName;

    public Task<bool> TryConsumeAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        return _context.TryConsumeAsync(permits, cancellationToken);
    }

    public Task<int> GetAvailablePermitsAsync(CancellationToken cancellationToken = default)
    {
        return _context.GetAvailablePermitsAsync(cancellationToken);
    }

    public Task<TimeSpan> GetWaitTimeAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        return _context.GetWaitTimeAsync(permits, cancellationToken);
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        return _context.ResetAsync(cancellationToken);
    }

    public void SetStrategy(IRateLimiter strategy)
    {
        _context.SetStrategy(strategy);
    }

    public object GetDebugInfo()
    {
        return new
        {
            CurrentStrategy = _context.CurrentStrategyName,
            AdapterType = "Universal"
        };
    }
} 