# API Reference

Welcome to the RateLimiter API reference documentation. This section provides detailed information about all public classes, interfaces, and methods in the RateLimiter library.

## Core Interfaces

### [IRateLimiter](RateLimiter.Interfaces.IRateLimiter.yml)
The main interface that all rate limiting strategies implement. Provides methods for consuming permits, checking availability, and managing rate limiter state.

**Key Methods:**
- `TryConsumeAsync(int permits, CancellationToken cancellationToken)` - Attempts to consume permits
- `GetAvailablePermitsAsync(CancellationToken cancellationToken)` - Gets current available permits
- `GetWaitTimeAsync(int permits, CancellationToken cancellationToken)` - Calculates wait time for permits
- `ResetAsync(CancellationToken cancellationToken)` - Resets the rate limiter state

## Strategy Implementations

### [TokenBucketStrategy](RateLimiter.Implementations.TokenBucketStrategy.yml)
Implements the token bucket algorithm, allowing burst traffic up to a configured capacity while maintaining a steady refill rate.

**Best For:** APIs that need to handle burst traffic while maintaining overall rate limits.

### [FixedWindowStrategy](RateLimiter.Implementations.FixedWindowStrategy.yml)
Implements fixed window counting, resetting the counter at fixed intervals.

**Best For:** Simple rate limiting scenarios where memory efficiency is important.

### [SlidingWindowStrategy](RateLimiter.Implementations.SlidingWindowStrategy.yml)
Implements sliding window counting, maintaining a rolling window of request timestamps for precise rate limiting.

**Best For:** Scenarios requiring strict, smooth rate limiting without burst allowances.

## Legacy Support

### [TokenBucket](RateLimiter.Implementations.TokenBucket.yml)
Legacy wrapper around TokenBucketStrategy for backward compatibility.

## Adapter Pattern

### [IRateLimiterAdapter](RateLimiter.Adapters.IRateLimiterAdapter.yml)
Extended interface providing additional functionality like strategy introspection and debugging.

### [TokenBucketAdapter](RateLimiter.Adapters.TokenBucketAdapter.yml)
Adapter implementation specifically for token bucket strategy with additional features.

### [UniversalRateLimiterAdapter](RateLimiter.Adapters.UniversalRateLimiterAdapter.yml)
Universal adapter that can wrap any IRateLimiter implementation.

## Configuration

### [RateLimiterOptions](RateLimiter.Configuration.RateLimiterOptions.yml)
Main configuration class for rate limiters, supporting all algorithms and their specific options.

**Key Properties:**
- `Algorithm` - The rate limiting algorithm to use
- `Capacity` - Maximum permits or requests allowed
- `RefillRate` - Rate at which tokens are refilled (TokenBucket only)
- `RefillPeriod` - Period for token refill (TokenBucket only)
- `WindowSize` - Size of the time window (Window-based algorithms)
- `DistributedCacheKey` - Key for distributed scenarios

### Algorithm-Specific Options

- [TokenBucketOptions](RateLimiter.Configuration.TokenBucketOptions.yml) - Token bucket specific configuration
- [FixedWindowOptions](RateLimiter.Configuration.FixedWindowOptions.yml) - Fixed window specific configuration  
- [SlidingWindowOptions](RateLimiter.Configuration.SlidingWindowOptions.yml) - Sliding window specific configuration

## Services and Factories

### [RateLimiterFactory](RateLimiter.Services.RateLimiterFactory.yml)
Factory class for creating rate limiter instances based on configuration.

### [RateLimiterContext](RateLimiter.Services.RateLimiterContext.yml)
Context class providing strategy pattern implementation for runtime algorithm switching.

### [ServiceCollectionExtensions](RateLimiter.Extensions.ServiceCollectionExtensions.yml)
Extension methods for registering rate limiter services with dependency injection containers.

## Namespaces

- **[RateLimiter.Interfaces](RateLimiter.Interfaces.yml)** - Core interfaces
- **[RateLimiter.Implementations](RateLimiter.Implementations.yml)** - Rate limiting algorithm implementations
- **[RateLimiter.Adapters](RateLimiter.Adapters.yml)** - Adapter pattern implementations
- **[RateLimiter.Configuration](RateLimiter.Configuration.yml)** - Configuration classes and options
- **[RateLimiter.Services](RateLimiter.Services.yml)** - Service classes and factories
- **[RateLimiter.Extensions](RateLimiter.Extensions.yml)** - Dependency injection extensions

## Quick Start

```csharp
// Configure in dependency injection
services.Configure<RateLimiterOptions>(options =>
{
    options.Algorithm = RateLimitingAlgorithm.TokenBucket;
    options.Capacity = 100;
    options.RefillRate = 10;
    options.RefillPeriod = TimeSpan.FromSeconds(1);
});

services.AddRateLimiter();

// Use in your service
public class MyService
{
    private readonly IRateLimiter _rateLimiter;

    public MyService(IRateLimiter rateLimiter)
    {
        _rateLimiter = rateLimiter;
    }

    public async Task<bool> ProcessRequestAsync()
    {
        return await _rateLimiter.TryConsumeAsync();
    }
}
```

## Thread Safety

All rate limiter implementations are thread-safe and designed for high-concurrency scenarios. The library uses appropriate locking mechanisms and concurrent data structures to ensure correctness under load.

## Performance Characteristics

| Algorithm | Memory Usage | CPU Usage | Accuracy | Burst Handling |
|-----------|--------------|-----------|----------|----------------|
| Token Bucket | Low | Very Low | High | Excellent |
| Fixed Window | Very Low | Very Low | Good | Poor |
| Sliding Window | High | Higher | Excellent | Good |

For detailed performance guidance, see the [Performance Considerations](../articles/performance.md) guide.