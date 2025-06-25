# Architecture Guide

This guide explains the architectural patterns and design principles used in the RateLimiter library.

## Design Patterns

### Strategy Pattern

The Strategy pattern allows the rate limiting algorithm to be selected at runtime:

```csharp
// IRateLimiter is the strategy interface
public interface IRateLimiter
{
    Task<bool> TryConsumeAsync(int permits = 1, CancellationToken cancellationToken = default);
    Task<int> GetAvailablePermitsAsync(CancellationToken cancellationToken = default);
    Task<TimeSpan> GetWaitTimeAsync(int permits = 1, CancellationToken cancellationToken = default);
    Task ResetAsync(CancellationToken cancellationToken = default);
}

// Concrete strategies
public class TokenBucketStrategy : IRateLimiter { /* ... */ }
public class FixedWindowStrategy : IRateLimiter { /* ... */ }
public class SlidingWindowStrategy : IRateLimiter { /* ... */ }
```

**Benefits:**
- Easy to switch algorithms at runtime
- Each algorithm is independently testable
- New algorithms can be added without modifying existing code

### Adapter Pattern

The Adapter pattern provides backward compatibility and legacy interface support:

```csharp
public interface IRateLimiterAdapter : IRateLimiter
{
    string StrategyTypeName { get; }
    void SetStrategy(IRateLimiter strategy);
    string GetDebugInfo();
}

public class TokenBucketAdapter : IRateLimiterAdapter
{
    private readonly TokenBucketStrategy _strategy;

    public TokenBucketAdapter(IOptions<RateLimiterOptions> options)
    {
        _strategy = new TokenBucketStrategy(options);
    }

    // Adapter methods delegate to the strategy
    public Task<bool> TryConsumeAsync(int permits = 1, CancellationToken cancellationToken = default)
        => _strategy.TryConsumeAsync(permits, cancellationToken);
}
```

**Benefits:**
- Maintains compatibility with legacy code
- Provides additional features (debug info, strategy introspection)
- Allows gradual migration to new interfaces

### Factory Pattern

The Factory pattern creates appropriate rate limiter instances based on configuration:

```csharp
public class RateLimiterFactory
{
    private readonly IOptions<RateLimiterOptions> _options;

    public RateLimiterFactory(IOptions<RateLimiterOptions> options)
    {
        _options = options;
    }

    public IRateLimiter CreateRateLimiter()
    {
        return _options.Value.Algorithm switch
        {
            RateLimitingAlgorithm.TokenBucket => new TokenBucketStrategy(_options),
            RateLimitingAlgorithm.FixedWindow => new FixedWindowStrategy(_options),
            RateLimitingAlgorithm.SlidingWindow => new SlidingWindowStrategy(_options),
            _ => throw new ArgumentException($"Unknown algorithm: {_options.Value.Algorithm}")
        };
    }
}
```

**Benefits:**
- Centralizes object creation logic
- Supports configuration-driven instantiation
- Easy to extend with new algorithms

### Options Pattern

The Options pattern provides type-safe configuration management:

```csharp
public class RateLimiterOptions
{
    public const string SectionName = "RateLimiter";

    public RateLimitingAlgorithm Algorithm { get; set; } = RateLimitingAlgorithm.TokenBucket;
    public int Capacity { get; set; } = 100;
    public double RefillRate { get; set; } = 10;
    public TimeSpan RefillPeriod { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan WindowSize { get; set; } = TimeSpan.FromMinutes(1);
    public string? DistributedCacheKey { get; set; }
}
```

**Benefits:**
- Type-safe configuration
- Supports validation
- Integrates with ASP.NET Core configuration system

## Architectural Layers

### 1. Interface Layer
```
┌─────────────────────────────────────┐
│           IRateLimiter              │
│         (Core Interface)            │
└─────────────────────────────────────┘
```

### 2. Strategy Layer
```
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│ TokenBucket     │ │ FixedWindow     │ │ SlidingWindow   │
│ Strategy        │ │ Strategy        │ │ Strategy        │
└─────────────────┘ └─────────────────┘ └─────────────────┘
```

### 3. Adapter Layer
```
┌─────────────────┐ ┌─────────────────┐
│ TokenBucket     │ │ Universal       │
│ Adapter         │ │ Adapter         │
└─────────────────┘ └─────────────────┘
```

### 4. Service Layer
```
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│ RateLimiter     │ │ RateLimiter     │ │ Service         │
│ Factory         │ │ Context         │ │ Extensions      │
└─────────────────┘ └─────────────────┘ └─────────────────┘
```

## Thread Safety

All rate limiter implementations are designed to be thread-safe:

### Lock-Free Optimizations
```csharp
public class TokenBucketStrategy : IRateLimiter
{
    private readonly object _lock = new object();
    private double _tokens;
    private DateTime _lastRefill;

    public async Task<bool> TryConsumeAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            Refill();
            
            if (_tokens >= permits)
            {
                _tokens -= permits;
                return true;
            }
            
            return false;
        }
    }
}
```

### Concurrent Collections
```csharp
// For sliding window, using concurrent data structures
private readonly ConcurrentQueue<DateTime> _requestTimes = new();
```

## Extension Points

### Custom Algorithms

To add a new rate limiting algorithm:

1. Implement `IRateLimiter`:
```csharp
public class CustomRateLimiter : IRateLimiter
{
    public async Task<bool> TryConsumeAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        // Your custom logic here
        throw new NotImplementedException();
    }

    // Implement other methods...
}
```

2. Add to the factory:
```csharp
public enum RateLimitingAlgorithm
{
    TokenBucket,
    FixedWindow,
    SlidingWindow,
    Custom // Add your algorithm
}

// Update factory
public IRateLimiter CreateRateLimiter()
{
    return _options.Value.Algorithm switch
    {
        // ... existing cases
        RateLimitingAlgorithm.Custom => new CustomRateLimiter(_options),
        _ => throw new ArgumentException($"Unknown algorithm: {_options.Value.Algorithm}")
    };
}
```

### Custom Storage Backends

For distributed scenarios, you can implement custom storage:

```csharp
public interface IRateLimiterStorage
{
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task<bool> TryLockAsync(string key, TimeSpan timeout);
    Task ReleaseLockAsync(string key);
}

public class RedisRateLimiterStorage : IRateLimiterStorage
{
    // Implementation using Redis
}
```

## Error Handling Strategy

### Graceful Degradation
```csharp
public class ResilientRateLimiter : IRateLimiter
{
    private readonly IRateLimiter _primary;
    private readonly IRateLimiter _fallback;

    public async Task<bool> TryConsumeAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primary.TryConsumeAsync(permits, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log error and use fallback
            return await _fallback.TryConsumeAsync(permits, cancellationToken);
        }
    }
}
```

### Circuit Breaker Pattern
```csharp
public class CircuitBreakerRateLimiter : IRateLimiter
{
    private readonly IRateLimiter _inner;
    private readonly CircuitBreaker _circuitBreaker;

    public async Task<bool> TryConsumeAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        if (_circuitBreaker.State == CircuitBreakerState.Open)
        {
            // Fail fast
            return false;
        }

        try
        {
            var result = await _inner.TryConsumeAsync(permits, cancellationToken);
            _circuitBreaker.RecordSuccess();
            return result;
        }
        catch (Exception ex)
        {
            _circuitBreaker.RecordFailure();
            throw;
        }
    }
}
```

## Testing Architecture

### Unit Testing
```csharp
[TestFixture]
public class TokenBucketStrategyTests
{
    private TokenBucketStrategy _rateLimiter;
    private RateLimiterOptions _options;

    [SetUp]
    public void Setup()
    {
        _options = new RateLimiterOptions
        {
            Algorithm = RateLimitingAlgorithm.TokenBucket,
            Capacity = 10,
            RefillRate = 1,
            RefillPeriod = TimeSpan.FromSeconds(1)
        };
        _rateLimiter = new TokenBucketStrategy(Options.Create(_options));
    }

    [Test]
    public async Task ShouldAllowRequestsUnderCapacity()
    {
        // Test implementation
    }
}
```

### Integration Testing
```csharp
[TestFixture]
public class RateLimiterIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [SetUp]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.Configure<RateLimiterOptions>(options =>
                    {
                        options.Capacity = 5;
                        options.RefillRate = 1;
                        options.RefillPeriod = TimeSpan.FromSeconds(1);
                    });
                });
            });
        _client = _factory.CreateClient();
    }
}
```

## Deployment Considerations

### Distributed Deployments
- Use Redis for shared state
- Configure appropriate timeout values
- Consider network partitions and failover scenarios

### Performance Monitoring
- Track rate limiter hit rates
- Monitor response times
- Alert on error rates

### Configuration Management
- Use environment-specific configurations
- Support hot configuration reloading
- Validate configuration at startup