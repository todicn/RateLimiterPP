using Microsoft.Extensions.Options;
using RateLimiter.Configuration;
using RateLimiter.Interfaces;

namespace RateLimiter.Implementations;

/// <summary>
/// Sliding window rate limiter strategy implementation.
/// 
/// The sliding window algorithm divides the time window into smaller sub-windows
/// and tracks requests across these sub-windows for more accurate rate limiting.
/// </summary>
public class SlidingWindowStrategy : IRateLimiter
{
    private readonly SlidingWindowOptions _options;
    private readonly object _lock = new();
    
    private readonly int[] _subWindowCounts;
    private readonly TimeSpan _subWindowDuration;
    private DateTime _lastUpdateTime;

    public SlidingWindowStrategy(IOptions<RateLimiterOptions> options)
    {
        _options = options.Value.SlidingWindow;
        _subWindowCounts = new int[_options.SubWindows];
        _subWindowDuration = TimeSpan.FromSeconds((double)_options.WindowSizeInSeconds / _options.SubWindows);
        _lastUpdateTime = DateTime.UtcNow;
    }

    public Task<bool> TryConsumeAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        if (permits <= 0)
            throw new ArgumentException("Permits must be positive", nameof(permits));

        lock (_lock)
        {
            UpdateSubWindows();
            
            var currentTotal = GetCurrentTotal();
            
            if (currentTotal + permits <= _options.MaxRequests)
            {
                var currentSubWindow = GetCurrentSubWindowIndex();
                _subWindowCounts[currentSubWindow] += permits;
                return Task.FromResult(true);
            }
            
            return Task.FromResult(false);
        }
    }

    public Task<int> GetAvailablePermitsAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            UpdateSubWindows();
            var currentTotal = GetCurrentTotal();
            return Task.FromResult(Math.Max(0, _options.MaxRequests - currentTotal));
        }
    }

    public Task<TimeSpan> GetWaitTimeAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        if (permits <= 0)
            throw new ArgumentException("Permits must be positive", nameof(permits));

        lock (_lock)
        {
            UpdateSubWindows();
            
            var currentTotal = GetCurrentTotal();
            
            if (currentTotal + permits <= _options.MaxRequests)
                return Task.FromResult(TimeSpan.Zero);
            
            // Estimate wait time based on sub-window duration
            // In a real implementation, you might want more sophisticated logic
            return Task.FromResult(_subWindowDuration);
        }
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            Array.Clear(_subWindowCounts, 0, _subWindowCounts.Length);
            _lastUpdateTime = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates sub-windows by clearing expired ones.
    /// Must be called while holding the lock.
    /// </summary>
    private void UpdateSubWindows()
    {
        var now = DateTime.UtcNow;
        var elapsed = now - _lastUpdateTime;
        
        if (elapsed >= _subWindowDuration)
        {
            var subWindowsToShift = Math.Min((int)(elapsed.TotalMilliseconds / _subWindowDuration.TotalMilliseconds), _options.SubWindows);
            
            // Shift the sub-windows (move old data out)
            if (subWindowsToShift >= _options.SubWindows)
            {
                // Clear all if we've moved beyond the entire window
                Array.Clear(_subWindowCounts, 0, _subWindowCounts.Length);
            }
            else
            {
                // Shift sub-windows
                for (int i = 0; i < _options.SubWindows - subWindowsToShift; i++)
                {
                    _subWindowCounts[i] = _subWindowCounts[i + subWindowsToShift];
                }
                
                // Clear the newest sub-windows
                for (int i = _options.SubWindows - subWindowsToShift; i < _options.SubWindows; i++)
                {
                    _subWindowCounts[i] = 0;
                }
            }
            
            _lastUpdateTime = now;
        }
    }

    /// <summary>
    /// Gets the current sub-window index based on time.
    /// Must be called while holding the lock.
    /// </summary>
    private int GetCurrentSubWindowIndex()
    {
        var timeSinceLastUpdate = DateTime.UtcNow - _lastUpdateTime;
        var subWindowIndex = (int)(timeSinceLastUpdate.TotalMilliseconds / _subWindowDuration.TotalMilliseconds);
        return Math.Min(subWindowIndex, _options.SubWindows - 1);
    }

    /// <summary>
    /// Gets the total count across all sub-windows.
    /// Must be called while holding the lock.
    /// </summary>
    private int GetCurrentTotal()
    {
        return _subWindowCounts.Sum();
    }

    /// <summary>
    /// Gets the current sub-window counts (for testing/debugging purposes)
    /// </summary>
    public int[] CurrentSubWindowCounts
    {
        get
        {
            lock (_lock)
            {
                UpdateSubWindows();
                return (int[])_subWindowCounts.Clone();
            }
        }
    }
} 