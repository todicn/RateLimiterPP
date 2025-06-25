namespace RateLimiter.Interfaces;

/// <summary>
/// Interface for rate limiting implementations
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Attempts to consume the specified number of permits
    /// </summary>
    /// <param name="permits">Number of permits to consume (default: 1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if permits were consumed, false if rate limit exceeded</returns>
    Task<bool> TryConsumeAsync(int permits = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current number of available permits
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of available permits</returns>
    Task<int> GetAvailablePermitsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the time to wait before the next permit becomes available
    /// </summary>
    /// <param name="permits">Number of permits needed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Time to wait, or TimeSpan.Zero if permits are available now</returns>
    Task<TimeSpan> GetWaitTimeAsync(int permits = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the rate limiter state
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ResetAsync(CancellationToken cancellationToken = default);
} 