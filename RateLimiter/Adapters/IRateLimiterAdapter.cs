using RateLimiter.Interfaces;

namespace RateLimiter.Adapters;

/// <summary>
/// Adapter interface for bridging between different rate limiter implementations.
/// This allows legacy code to work with new strategy implementations.
/// </summary>
public interface IRateLimiterAdapter : IRateLimiter
{
    /// <summary>
    /// Gets the underlying strategy type name
    /// </summary>
    string StrategyTypeName { get; }

    /// <summary>
    /// Gets additional debugging information specific to the implementation
    /// </summary>
    object GetDebugInfo();

    /// <summary>
    /// Sets a new underlying strategy (for runtime switching)
    /// </summary>
    /// <param name="strategy">The new strategy to use</param>
    void SetStrategy(IRateLimiter strategy);
} 