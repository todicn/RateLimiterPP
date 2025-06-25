using Microsoft.Extensions.Options;
using RateLimiter.Configuration;
using RateLimiter.Interfaces;

namespace RateLimiter.Implementations;

/// <summary>
/// Fixed window rate limiter strategy implementation.
/// 
/// The fixed window algorithm maintains a counter for a fixed time window.
/// The counter resets at the start of each window period.
/// </summary>
public class FixedWindowStrategy : IRateLimiter
{
    private readonly FixedWindowOptions _options;
    private readonly object _lock = new();
    
    private int _requestCount;
    private DateTime _windowStart;

    public FixedWindowStrategy(IOptions<RateLimiterOptions> options)
    {
        _options = options.Value.FixedWindow;
        _windowStart = DateTime.UtcNow;
        _requestCount = 0;
    }

    public Task<bool> TryConsumeAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        if (permits <= 0)
            throw new ArgumentException("Permits must be positive", nameof(permits));

        lock (_lock)
        {
            var now = DateTime.UtcNow;
            
            // Check if we need to reset the window
            if (now >= _windowStart.AddSeconds(_options.WindowSizeInSeconds))
            {
                _windowStart = now;
                _requestCount = 0;
            }
            
            // Check if we can accommodate the request
            if (_requestCount + permits <= _options.MaxRequests)
            {
                _requestCount += permits;
                return Task.FromResult(true);
            }
            
            return Task.FromResult(false);
        }
    }

    public Task<int> GetAvailablePermitsAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            
            // Check if we need to reset the window
            if (now >= _windowStart.AddSeconds(_options.WindowSizeInSeconds))
            {
                _windowStart = now;
                _requestCount = 0;
            }
            
            return Task.FromResult(_options.MaxRequests - _requestCount);
        }
    }

    public Task<TimeSpan> GetWaitTimeAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        if (permits <= 0)
            throw new ArgumentException("Permits must be positive", nameof(permits));

        lock (_lock)
        {
            var now = DateTime.UtcNow;
            
            // Check if we need to reset the window
            if (now >= _windowStart.AddSeconds(_options.WindowSizeInSeconds))
            {
                _windowStart = now;
                _requestCount = 0;
            }
            
            // If we have enough permits available, no wait time
            if (_requestCount + permits <= _options.MaxRequests)
                return Task.FromResult(TimeSpan.Zero);
            
            // Calculate time until next window starts
            var nextWindow = _windowStart.AddSeconds(_options.WindowSizeInSeconds);
            return Task.FromResult(nextWindow - now);
        }
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _windowStart = DateTime.UtcNow;
            _requestCount = 0;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current request count and window information (for testing/debugging purposes)
    /// </summary>
    public (int RequestCount, DateTime WindowStart, DateTime WindowEnd) CurrentWindow
    {
        get
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                
                // Check if we need to reset the window
                if (now >= _windowStart.AddSeconds(_options.WindowSizeInSeconds))
                {
                    _windowStart = now;
                    _requestCount = 0;
                }
                
                return (_requestCount, _windowStart, _windowStart.AddSeconds(_options.WindowSizeInSeconds));
            }
        }
    }
} 