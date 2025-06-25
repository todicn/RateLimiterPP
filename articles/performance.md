# Performance Considerations

This guide covers performance optimization strategies when using the RateLimiter library.

## Algorithm Performance Comparison

### Token Bucket
- **Memory Usage**: Low (constant memory per instance)
- **CPU Usage**: Very low (simple arithmetic operations)
- **Accuracy**: High
- **Burst Handling**: Excellent (designed for bursts)
- **Best For**: APIs that can handle burst traffic

### Fixed Window
- **Memory Usage**: Very low (just a counter and timestamp)
- **CPU Usage**: Very low (minimal operations)
- **Accuracy**: Good (window boundary issues)
- **Burst Handling**: Poor (can allow 2x capacity at boundaries)
- **Best For**: Simple rate limiting with minimal overhead

### Sliding Window
- **Memory Usage**: High (stores individual request timestamps)
- **CPU Usage**: Higher (must clean up expired entries)
- **Accuracy**: Excellent (most precise)
- **Burst Handling**: Good (smooth distribution)
- **Best For**: Strict rate limiting requirements

## Optimization Strategies

### 1. Choose the Right Algorithm

```csharp
// For high-throughput scenarios with burst tolerance
services.Configure<RateLimiterOptions>(options =>
{
    options.Algorithm = RateLimitingAlgorithm.TokenBucket;
    options.Capacity = 1000;
    options.RefillRate = 100;
    options.RefillPeriod = TimeSpan.FromSeconds(1);
});

// For minimal memory usage
services.Configure<RateLimiterOptions>(options =>
{
    options.Algorithm = RateLimitingAlgorithm.FixedWindow;
    options.Capacity = 100;
    options.WindowSize = TimeSpan.FromMinutes(1);
});

// For strict accuracy requirements
services.Configure<RateLimiterOptions>(options =>
{
    options.Algorithm = RateLimitingAlgorithm.SlidingWindow;
    options.Capacity = 100;
    options.WindowSize = TimeSpan.FromMinutes(1);
});
```

### 2. Minimize Lock Contention

The library uses optimized locking strategies, but you can help by:

```csharp
// Use separate rate limiters for different operations
public class OptimizedService
{
    private readonly IRateLimiter _readLimiter;
    private readonly IRateLimiter _writeLimiter;

    public OptimizedService(IRateLimiterFactory factory)
    {
        _readLimiter = factory.CreateRateLimiter("ReadOperations");
        _writeLimiter = factory.CreateRateLimiter("WriteOperations");
    }

    // This avoids contention between read and write operations
}
```

### 3. Batch Operations When Possible

```csharp
public async Task<bool> ProcessBatchAsync(int itemCount)
{
    // Try to consume permits for the entire batch at once
    if (await _rateLimiter.TryConsumeAsync(itemCount))
    {
        // Process all items
        return true;
    }

    // Fallback: check available permits and process what we can
    var available = await _rateLimiter.GetAvailablePermitsAsync();
    if (available > 0)
    {
        await _rateLimiter.TryConsumeAsync(available);
        // Process 'available' number of items
        return false; // Partial processing
    }

    return false; // No processing possible
}
```

### 4. Avoid Frequent Polling

```csharp
// DON'T do this - wasteful polling
public async Task BadPatternAsync()
{
    while (!await _rateLimiter.TryConsumeAsync())
    {
        await Task.Delay(10); // Wasteful
    }
    // Process request
}

// DO this instead - wait for the calculated time
public async Task GoodPatternAsync()
{
    var waitTime = await _rateLimiter.GetWaitTimeAsync();
    if (waitTime > TimeSpan.Zero)
    {
        await Task.Delay(waitTime);
    }
    
    await _rateLimiter.TryConsumeAsync();
    // Process request
}
```

## Memory Management

### Sliding Window Cleanup

The sliding window implementation automatically cleans up expired entries:

```csharp
// The library handles this automatically, but you can help by:

// 1. Using appropriate window sizes (don't make them too large)
services.Configure<RateLimiterOptions>(options =>
{
    options.Algorithm = RateLimitingAlgorithm.SlidingWindow;
    options.Capacity = 100;
    options.WindowSize = TimeSpan.FromMinutes(5); // Reasonable size
});

// 2. Calling ResetAsync() when appropriate (e.g., user logout)
public async Task OnUserLogoutAsync(string userId)
{
    var userLimiter = _userLimiters.GetValueOrDefault(userId);
    if (userLimiter != null)
    {
        await userLimiter.ResetAsync();
        _userLimiters.TryRemove(userId, out _);
    }
}
```

### Per-User Rate Limiters

```csharp
public class EfficientUserRateLimitingService
{
    private readonly ConcurrentDictionary<string, IRateLimiter> _userLimiters;
    private readonly Timer _cleanupTimer;

    public EfficientUserRateLimitingService()
    {
        _userLimiters = new ConcurrentDictionary<string, IRateLimiter>();
        
        // Cleanup inactive limiters every 10 minutes
        _cleanupTimer = new Timer(CleanupInactiveLimiters, null, 
            TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
    }

    private void CleanupInactiveLimiters(object state)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-30);
        var inactiveUsers = new List<string>();

        foreach (var kvp in _userLimiters)
        {
            // Check if limiter has been inactive
            if (IsInactive(kvp.Value, cutoff))
            {
                inactiveUsers.Add(kvp.Key);
            }
        }

        foreach (var userId in inactiveUsers)
        {
            _userLimiters.TryRemove(userId, out _);
        }
    }
}
```

## Distributed Performance

### Redis Optimization

```csharp
services.Configure<RateLimiterOptions>(options =>
{
    options.Algorithm = RateLimitingAlgorithm.TokenBucket;
    options.Capacity = 1000;
    options.RefillRate = 100;
    options.RefillPeriod = TimeSpan.FromSeconds(1);
    // Use a descriptive key
    options.DistributedCacheKey = "MyApp:API:RateLimit";
});

// Configure Redis for better performance
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "MyApp";
    
    // Optimize for rate limiting workloads
    options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
    {
        EndPoints = { "localhost:6379" },
        AbortOnConnectFail = false,
        ConnectRetry = 3,
        ConnectTimeout = 5000,
        ResponseTimeout = 5000,
        // Use connection pooling
        KeepAlive = 60
    };
});
```

## Monitoring and Metrics

```csharp
public class MonitoredRateLimiter : IRateLimiter
{
    private readonly IRateLimiter _inner;
    private readonly IMetrics _metrics;

    public MonitoredRateLimiter(IRateLimiter inner, IMetrics metrics)
    {
        _inner = inner;
        _metrics = metrics;
    }

    public async Task<bool> TryConsumeAsync(int permits = 1, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await _inner.TryConsumeAsync(permits, cancellationToken);
        stopwatch.Stop();

        _metrics.Record("rate_limiter.try_consume.duration", stopwatch.ElapsedMilliseconds);
        _metrics.Counter("rate_limiter.requests.total").Increment();
        
        if (result)
        {
            _metrics.Counter("rate_limiter.requests.allowed").Increment();
        }
        else
        {
            _metrics.Counter("rate_limiter.requests.denied").Increment();
        }

        return result;
    }

    // Implement other methods similarly...
}
```

## Performance Testing

```csharp
[Test]
public async Task PerformanceTest()
{
    var options = Options.Create(new RateLimiterOptions
    {
        Algorithm = RateLimitingAlgorithm.TokenBucket,
        Capacity = 10000,
        RefillRate = 1000,
        RefillPeriod = TimeSpan.FromSeconds(1)
    });

    var rateLimiter = new TokenBucketStrategy(options);
    var tasks = new List<Task<bool>>();

    var stopwatch = Stopwatch.StartNew();

    // Simulate 1000 concurrent requests
    for (int i = 0; i < 1000; i++)
    {
        tasks.Add(rateLimiter.TryConsumeAsync());
    }

    var results = await Task.WhenAll(tasks);
    stopwatch.Stop();

    Console.WriteLine($"Processed 1000 requests in {stopwatch.ElapsedMilliseconds}ms");
    Console.WriteLine($"Allowed: {results.Count(r => r)}");
    Console.WriteLine($"Denied: {results.Count(r => !r)}");
}
```

## Best Practices Summary

1. **Choose the right algorithm** for your use case
2. **Use separate limiters** for different operations to reduce contention
3. **Implement proper cleanup** for per-user limiters
4. **Monitor performance** with metrics
5. **Test under load** to validate performance characteristics
6. **Configure Redis appropriately** for distributed scenarios
7. **Avoid polling** - use calculated wait times instead