# Configuration Guide

This guide covers all configuration options available in the RateLimiter library.

## Configuration Options

### RateLimiterOptions

The main configuration class that defines how your rate limiter behaves:

```csharp
public class RateLimiterOptions
{
    public RateLimitingAlgorithm Algorithm { get; set; }
    public int Capacity { get; set; }
    public double RefillRate { get; set; }
    public TimeSpan RefillPeriod { get; set; }
    public TimeSpan WindowSize { get; set; }
    public string? DistributedCacheKey { get; set; }
}
```

### Algorithm Types

#### TokenBucket
```csharp
services.Configure<RateLimiterOptions>(options =>
{
    options.Algorithm = RateLimitingAlgorithm.TokenBucket;
    options.Capacity = 100;        // Maximum tokens
    options.RefillRate = 10;       // Tokens per period
    options.RefillPeriod = TimeSpan.FromSeconds(1);
});
```

#### FixedWindow
```csharp
services.Configure<RateLimiterOptions>(options =>
{
    options.Algorithm = RateLimitingAlgorithm.FixedWindow;
    options.Capacity = 100;        // Requests per window
    options.WindowSize = TimeSpan.FromMinutes(1);
});
```

#### SlidingWindow
```csharp
services.Configure<RateLimiterOptions>(options =>
{
    options.Algorithm = RateLimitingAlgorithm.SlidingWindow;
    options.Capacity = 100;        // Requests per window
    options.WindowSize = TimeSpan.FromMinutes(1);
});
```

## Configuration Sources

### appsettings.json
```json
{
  "RateLimiter": {
    "Algorithm": "TokenBucket",
    "Capacity": 100,
    "RefillRate": 10,
    "RefillPeriod": "00:00:01",
    "WindowSize": "00:01:00",
    "DistributedCacheKey": "MyApp:RateLimit"
  }
}
```

### Environment Variables
```bash
RateLimiter__Algorithm=TokenBucket
RateLimiter__Capacity=100
RateLimiter__RefillRate=10
RateLimiter__RefillPeriod=00:00:01
```

### Code-based Configuration
```csharp
services.Configure<RateLimiterOptions>(options =>
{
    options.Algorithm = RateLimitingAlgorithm.TokenBucket;
    options.Capacity = 100;
    options.RefillRate = 10;
    options.RefillPeriod = TimeSpan.FromSeconds(1);
});
```

## Advanced Configuration

### Multiple Rate Limiters
```csharp
// Configure different rate limiters for different scenarios
services.Configure<RateLimiterOptions>("ApiRateLimit", options =>
{
    options.Algorithm = RateLimitingAlgorithm.TokenBucket;
    options.Capacity = 1000;
    options.RefillRate = 100;
    options.RefillPeriod = TimeSpan.FromSeconds(1);
});

services.Configure<RateLimiterOptions>("AuthRateLimit", options =>
{
    options.Algorithm = RateLimitingAlgorithm.FixedWindow;
    options.Capacity = 5;
    options.WindowSize = TimeSpan.FromMinutes(1);
});
```

### Distributed Scenarios
```csharp
services.Configure<RateLimiterOptions>(options =>
{
    options.Algorithm = RateLimitingAlgorithm.TokenBucket;
    options.Capacity = 100;
    options.RefillRate = 10;
    options.RefillPeriod = TimeSpan.FromSeconds(1);
    options.DistributedCacheKey = "MyApp:GlobalRateLimit";
});

// Add Redis distributed cache
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
```

## Validation

The library automatically validates configuration:

- `Capacity` must be greater than 0
- `RefillRate` must be greater than 0 for TokenBucket
- `RefillPeriod` must be greater than TimeSpan.Zero for TokenBucket
- `WindowSize` must be greater than TimeSpan.Zero for window-based algorithms

Invalid configurations will throw `ArgumentException` during service registration.