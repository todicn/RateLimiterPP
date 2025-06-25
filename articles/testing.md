# Testing Guide

This guide covers comprehensive testing strategies for the RateLimiter library.

## Unit Testing

### Testing Rate Limiter Algorithms

#### Token Bucket Testing
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
            RefillRate = 5,
            RefillPeriod = TimeSpan.FromSeconds(1)
        };
        _rateLimiter = new TokenBucketStrategy(Options.Create(_options));
    }

    [Test]
    public async Task ShouldAllowRequestsUpToCapacity()
    {
        // Should allow up to capacity
        for (int i = 0; i < 10; i++)
        {
            Assert.IsTrue(await _rateLimiter.TryConsumeAsync());
        }

        // Should deny the next request
        Assert.IsFalse(await _rateLimiter.TryConsumeAsync());
    }

    [Test]
    public async Task ShouldRefillTokensOverTime()
    {
        // Consume all tokens
        for (int i = 0; i < 10; i++)
        {
            await _rateLimiter.TryConsumeAsync();
        }

        // Wait for refill
        await Task.Delay(TimeSpan.FromSeconds(1.1));

        // Should have 5 new tokens (refill rate)
        for (int i = 0; i < 5; i++)
        {
            Assert.IsTrue(await _rateLimiter.TryConsumeAsync(), $"Token {i + 1} should be available");
        }

        Assert.IsFalse(await _rateLimiter.TryConsumeAsync(), "No more tokens should be available");
    }

    [Test]
    public async Task ShouldHandleBurstTraffic()
    {
        // Allow burst up to capacity
        var tasks = new List<Task<bool>>();
        for (int i = 0; i < 15; i++)
        {
            tasks.Add(_rateLimiter.TryConsumeAsync());
        }

        var results = await Task.WhenAll(tasks);
        var allowed = results.Count(r => r);
        var denied = results.Count(r => !r);

        Assert.AreEqual(10, allowed, "Should allow exactly 10 requests");
        Assert.AreEqual(5, denied, "Should deny exactly 5 requests");
    }

    [Test]
    public async Task GetAvailablePermitsAsync_ShouldReturnCorrectCount()
    {
        // Initially should have full capacity
        var available = await _rateLimiter.GetAvailablePermitsAsync();
        Assert.AreEqual(10, available);

        // After consuming some tokens
        await _rateLimiter.TryConsumeAsync(3);
        available = await _rateLimiter.GetAvailablePermitsAsync();
        Assert.AreEqual(7, available);
    }

    [Test]
    public async Task GetWaitTimeAsync_ShouldCalculateCorrectWaitTime()
    {
        // Consume all tokens
        for (int i = 0; i < 10; i++)
        {
            await _rateLimiter.TryConsumeAsync();
        }

        var waitTime = await _rateLimiter.GetWaitTimeAsync();
        
        // Should need to wait approximately 1 second for next token
        Assert.IsTrue(waitTime.TotalMilliseconds > 900 && waitTime.TotalMilliseconds < 1100,
            $"Wait time should be ~1000ms, but was {waitTime.TotalMilliseconds}ms");
    }

    [Test]
    public async Task ResetAsync_ShouldRestoreFullCapacity()
    {
        // Consume some tokens
        for (int i = 0; i < 5; i++)
        {
            await _rateLimiter.TryConsumeAsync();
        }

        // Reset should restore full capacity
        await _rateLimiter.ResetAsync();

        var available = await _rateLimiter.GetAvailablePermitsAsync();
        Assert.AreEqual(10, available);
    }
}
```

#### Fixed Window Testing
```csharp
[TestFixture]
public class FixedWindowStrategyTests
{
    private FixedWindowStrategy _rateLimiter;

    [SetUp]
    public void Setup()
    {
        var options = new RateLimiterOptions
        {
            Algorithm = RateLimitingAlgorithm.FixedWindow,
            Capacity = 5,
            WindowSize = TimeSpan.FromSeconds(2)
        };
        _rateLimiter = new FixedWindowStrategy(Options.Create(options));
    }

    [Test]
    public async Task ShouldAllowRequestsInWindow()
    {
        for (int i = 0; i < 5; i++)
        {
            Assert.IsTrue(await _rateLimiter.TryConsumeAsync());
        }

        Assert.IsFalse(await _rateLimiter.TryConsumeAsync());
    }

    [Test]
    public async Task ShouldResetAtWindowBoundary()
    {
        // Consume all requests in current window
        for (int i = 0; i < 5; i++)
        {
            await _rateLimiter.TryConsumeAsync();
        }

        // Wait for window to reset
        await Task.Delay(TimeSpan.FromSeconds(2.1));

        // Should allow requests again
        Assert.IsTrue(await _rateLimiter.TryConsumeAsync());
    }
}
```

#### Sliding Window Testing
```csharp
[TestFixture]
public class SlidingWindowStrategyTests
{
    private SlidingWindowStrategy _rateLimiter;

    [SetUp]
    public void Setup()
    {
        var options = new RateLimiterOptions
        {
            Algorithm = RateLimitingAlgorithm.SlidingWindow,
            Capacity = 5,
            WindowSize = TimeSpan.FromSeconds(5)
        };
        _rateLimiter = new SlidingWindowStrategy(Options.Create(options));
    }

    [Test]
    public async Task ShouldMaintainSlidingWindow()
    {
        // Make requests at 1-second intervals
        for (int i = 0; i < 5; i++)
        {
            Assert.IsTrue(await _rateLimiter.TryConsumeAsync());
            if (i < 4) await Task.Delay(TimeSpan.FromSeconds(1));
        }

        // Immediate next request should be denied
        Assert.IsFalse(await _rateLimiter.TryConsumeAsync());

        // Wait 2 seconds (total 6 seconds from first request)
        await Task.Delay(TimeSpan.FromSeconds(2));

        // First request should have expired from window
        Assert.IsTrue(await _rateLimiter.TryConsumeAsync());
    }
}
```

## Concurrency Testing

### Thread Safety Tests
```csharp
[TestFixture]
public class ConcurrencyTests
{
    [Test]
    public async Task ShouldBehavCorrectlyUnderConcurrentLoad()
    {
        var options = Options.Create(new RateLimiterOptions
        {
            Algorithm = RateLimitingAlgorithm.TokenBucket,
            Capacity = 100,
            RefillRate = 10,
            RefillPeriod = TimeSpan.FromSeconds(1)
        });

        var rateLimiter = new TokenBucketStrategy(options);
        var tasks = new List<Task<bool>>();

        // Simulate 1000 concurrent requests
        for (int i = 0; i < 1000; i++)
        {
            tasks.Add(rateLimiter.TryConsumeAsync());
        }

        var results = await Task.WhenAll(tasks);
        var allowed = results.Count(r => r);
        var denied = results.Count(r => !r);

        // Should allow exactly 100 requests (capacity)
        Assert.AreEqual(100, allowed);
        Assert.AreEqual(900, denied);

        // Total state should be consistent
        var available = await rateLimiter.GetAvailablePermitsAsync();
        Assert.AreEqual(0, available);
    }

    [Test]
    public async Task ShouldHandleRaceConditions()
    {
        var options = Options.Create(new RateLimiterOptions
        {
            Algorithm = RateLimitingAlgorithm.TokenBucket,
            Capacity = 1,
            RefillRate = 1,
            RefillPeriod = TimeSpan.FromSeconds(1)
        });

        var rateLimiter = new TokenBucketStrategy(options);
        var successCount = 0;

        // Run many concurrent operations
        var tasks = Enumerable.Range(0, 100).Select(async _ =>
        {
            for (int i = 0; i < 10; i++)
            {
                if (await rateLimiter.TryConsumeAsync())
                {
                    Interlocked.Increment(ref successCount);
                }
                await Task.Delay(10);
            }
        });

        await Task.WhenAll(tasks);

        // Success count should be reasonable (allowing for some refill)
        Assert.IsTrue(successCount > 0 && successCount < 20, 
            $"Expected reasonable success count, got {successCount}");
    }
}
```

## Integration Testing

### ASP.NET Core Integration
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
                        options.Algorithm = RateLimitingAlgorithm.TokenBucket;
                        options.Capacity = 5;
                        options.RefillRate = 1;
                        options.RefillPeriod = TimeSpan.FromSeconds(1);
                    });
                    services.AddRateLimiter();
                });

                builder.Configure(app =>
                {
                    app.UseMiddleware<RateLimitingMiddleware>();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/test", () => "OK");
                    });
                });
            });

        _client = _factory.CreateClient();
    }

    [Test]
    public async Task ShouldApplyRateLimitingToEndpoints()
    {
        var responses = new List<HttpResponseMessage>();

        // Make requests up to limit
        for (int i = 0; i < 7; i++)
        {
            responses.Add(await _client.GetAsync("/test"));
        }

        var okResponses = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var rateLimitedResponses = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);

        Assert.AreEqual(5, okResponses, "Should allow 5 requests");
        Assert.AreEqual(2, rateLimitedResponses, "Should rate limit 2 requests");
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
```

### Redis Integration Testing
```csharp
[TestFixture]
public class RedisIntegrationTests
{
    private ServiceProvider _serviceProvider;
    private IRateLimiter _rateLimiter;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = "localhost:6379";
        });

        services.Configure<RateLimiterOptions>(options =>
        {
            options.Algorithm = RateLimitingAlgorithm.TokenBucket;
            options.Capacity = 10;
            options.RefillRate = 5;
            options.RefillPeriod = TimeSpan.FromSeconds(1);
            options.DistributedCacheKey = "Test:RateLimit";
        });

        services.AddRateLimiter();

        _serviceProvider = services.BuildServiceProvider();
        _rateLimiter = _serviceProvider.GetRequiredService<IRateLimiter>();
    }

    [Test]
    public async Task ShouldPersistStateInRedis()
    {
        // Consume some permits
        for (int i = 0; i < 5; i++)
        {
            await _rateLimiter.TryConsumeAsync();
        }

        // Create new rate limiter instance (simulating app restart)
        var newRateLimiter = _serviceProvider.GetRequiredService<IRateLimiter>();

        // Should maintain state from Redis
        var available = await newRateLimiter.GetAvailablePermitsAsync();
        Assert.AreEqual(5, available);
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider?.Dispose();
    }
}
```

## Performance Testing

### Benchmark Testing
```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class RateLimiterBenchmarks
{
    private TokenBucketStrategy _tokenBucket;
    private FixedWindowStrategy _fixedWindow;
    private SlidingWindowStrategy _slidingWindow;

    [GlobalSetup]
    public void Setup()
    {
        var tokenBucketOptions = Options.Create(new RateLimiterOptions
        {
            Algorithm = RateLimitingAlgorithm.TokenBucket,
            Capacity = 1000,
            RefillRate = 100,
            RefillPeriod = TimeSpan.FromSeconds(1)
        });

        var fixedWindowOptions = Options.Create(new RateLimiterOptions
        {
            Algorithm = RateLimitingAlgorithm.FixedWindow,
            Capacity = 1000,
            WindowSize = TimeSpan.FromSeconds(10)
        });

        var slidingWindowOptions = Options.Create(new RateLimiterOptions
        {
            Algorithm = RateLimitingAlgorithm.SlidingWindow,
            Capacity = 1000,
            WindowSize = TimeSpan.FromSeconds(10)
        });

        _tokenBucket = new TokenBucketStrategy(tokenBucketOptions);
        _fixedWindow = new FixedWindowStrategy(fixedWindowOptions);
        _slidingWindow = new SlidingWindowStrategy(slidingWindowOptions);
    }

    [Benchmark]
    public async Task<bool> TokenBucket_TryConsume()
    {
        return await _tokenBucket.TryConsumeAsync();
    }

    [Benchmark]
    public async Task<bool> FixedWindow_TryConsume()
    {
        return await _fixedWindow.TryConsumeAsync();
    }

    [Benchmark]
    public async Task<bool> SlidingWindow_TryConsume()
    {
        return await _slidingWindow.TryConsumeAsync();
    }
}
```

### Load Testing
```csharp
[TestFixture]
public class LoadTests
{
    [Test]
    public async Task ShouldHandleHighThroughput()
    {
        var options = Options.Create(new RateLimiterOptions
        {
            Algorithm = RateLimitingAlgorithm.TokenBucket,
            Capacity = 10000,
            RefillRate = 1000,
            RefillPeriod = TimeSpan.FromSeconds(1)
        });

        var rateLimiter = new TokenBucketStrategy(options);
        var stopwatch = Stopwatch.StartNew();
        var totalRequests = 100000;
        var allowedRequests = 0;

        var tasks = Enumerable.Range(0, totalRequests).Select(async _ =>
        {
            if (await rateLimiter.TryConsumeAsync())
            {
                Interlocked.Increment(ref allowedRequests);
            }
        });

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var throughput = totalRequests / stopwatch.Elapsed.TotalSeconds;

        Console.WriteLine($"Processed {totalRequests:N0} requests in {stopwatch.ElapsedMilliseconds:N0}ms");
        Console.WriteLine($"Throughput: {throughput:N0} requests/second");
        Console.WriteLine($"Allowed: {allowedRequests:N0} ({(double)allowedRequests/totalRequests:P2})");

        Assert.IsTrue(throughput > 10000, $"Throughput should be > 10,000 req/s, was {throughput:F0}");
    }
}
```

## Test Helpers and Utilities

### Time Mocking
```csharp
public interface ITimeProvider
{
    DateTime UtcNow { get; }
}

public class SystemTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public class MockTimeProvider : ITimeProvider
{
    public DateTime UtcNow { get; set; } = DateTime.UtcNow;

    public void Advance(TimeSpan timeSpan)
    {
        UtcNow = UtcNow.Add(timeSpan);
    }
}

// In your tests
[Test]
public async Task ShouldRefillAfterTimePasses()
{
    var mockTime = new MockTimeProvider();
    var rateLimiter = new TokenBucketStrategy(options, mockTime);

    // Consume all tokens
    for (int i = 0; i < 10; i++)
    {
        await rateLimiter.TryConsumeAsync();
    }

    // Advance time
    mockTime.Advance(TimeSpan.FromSeconds(1));

    // Should have new tokens
    Assert.IsTrue(await rateLimiter.TryConsumeAsync());
}
```

### Test Data Builders
```csharp
public class RateLimiterOptionsBuilder
{
    private readonly RateLimiterOptions _options = new();

    public RateLimiterOptionsBuilder WithTokenBucket(int capacity, double refillRate, TimeSpan refillPeriod)
    {
        _options.Algorithm = RateLimitingAlgorithm.TokenBucket;
        _options.Capacity = capacity;
        _options.RefillRate = refillRate;
        _options.RefillPeriod = refillPeriod;
        return this;
    }

    public RateLimiterOptionsBuilder WithFixedWindow(int capacity, TimeSpan windowSize)
    {
        _options.Algorithm = RateLimitingAlgorithm.FixedWindow;
        _options.Capacity = capacity;
        _options.WindowSize = windowSize;
        return this;
    }

    public RateLimiterOptions Build() => _options;
}

// Usage in tests
var options = new RateLimiterOptionsBuilder()
    .WithTokenBucket(capacity: 100, refillRate: 10, refillPeriod: TimeSpan.FromSeconds(1))
    .Build();
```

## Testing Best Practices

1. **Test All Algorithms**: Ensure each rate limiting algorithm is thoroughly tested
2. **Test Concurrency**: Verify thread safety under load
3. **Test Time Boundaries**: Test behavior at window boundaries and during refill periods
4. **Test Error Conditions**: Verify behavior when dependencies fail
5. **Performance Testing**: Benchmark different algorithms and configurations
6. **Integration Testing**: Test the full stack including middleware and DI
7. **Use Deterministic Time**: Mock time providers for predictable tests