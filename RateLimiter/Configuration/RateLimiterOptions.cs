namespace RateLimiter.Configuration;

/// <summary>
/// Configuration options for rate limiters
/// </summary>
public class RateLimiterOptions
{
    public const string SectionName = "RateLimiter";

    /// <summary>
    /// Token bucket configuration
    /// </summary>
    public TokenBucketOptions TokenBucket { get; set; } = new();

    /// <summary>
    /// Fixed window configuration
    /// </summary>
    public FixedWindowOptions FixedWindow { get; set; } = new();

    /// <summary>
    /// Sliding window configuration
    /// </summary>
    public SlidingWindowOptions SlidingWindow { get; set; } = new();
}

/// <summary>
/// Token bucket specific configuration
/// </summary>
public class TokenBucketOptions
{
    /// <summary>
    /// Maximum number of tokens the bucket can hold
    /// </summary>
    public int Capacity { get; set; } = 10;

    /// <summary>
    /// Rate at which tokens are refilled per second
    /// </summary>
    public double RefillRate { get; set; } = 1.0;
}

/// <summary>
/// Fixed window specific configuration
/// </summary>
public class FixedWindowOptions
{
    /// <summary>
    /// Maximum number of requests per window
    /// </summary>
    public int MaxRequests { get; set; } = 100;

    /// <summary>
    /// Window duration in seconds
    /// </summary>
    public int WindowSizeInSeconds { get; set; } = 60;
}

/// <summary>
/// Sliding window specific configuration
/// </summary>
public class SlidingWindowOptions
{
    /// <summary>
    /// Maximum number of requests in the sliding window
    /// </summary>
    public int MaxRequests { get; set; } = 100;

    /// <summary>
    /// Window duration in seconds
    /// </summary>
    public int WindowSizeInSeconds { get; set; } = 60;

    /// <summary>
    /// Number of sub-windows for sliding calculation
    /// </summary>
    public int SubWindows { get; set; } = 10;
} 