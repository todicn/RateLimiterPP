# Usage Examples

This page provides practical examples of how to use the RateLimiter library in different scenarios.

## Basic Usage

### Simple API Rate Limiting

```csharp
[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IRateLimiter _rateLimiter;

    public WeatherController(IRateLimiter rateLimiter)
    {
        _rateLimiter = rateLimiter;
    }

    [HttpGet]
    public async Task<IActionResult> GetWeather()
    {
        if (!await _rateLimiter.TryConsumeAsync())
        {
            return StatusCode(429, new { error = "Rate limit exceeded. Please try again later." });
        }

        // Your API logic here
        return Ok(new { temperature = 22, condition = "Sunny" });
    }
}
```

### Middleware Integration

```csharp
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimiter _rateLimiter;

    public RateLimitingMiddleware(RequestDelegate next, IRateLimiter rateLimiter)
    {
        _next = next;
        _rateLimiter = rateLimiter;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!await _rateLimiter.TryConsumeAsync())
        {
            context.Response.StatusCode = 429;
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }

        await _next(context);
    }
}

// In Startup.cs or Program.cs
app.UseMiddleware<RateLimitingMiddleware>();
```

## Advanced Scenarios

### Per-User Rate Limiting

```csharp
public class UserRateLimitingService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, IRateLimiter> _userLimiters;

    public UserRateLimitingService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _userLimiters = new ConcurrentDictionary<string, IRateLimiter>();
    }

    public async Task<bool> TryConsumeAsync(string userId)
    {
        var limiter = _userLimiters.GetOrAdd(userId, _ => 
            _serviceProvider.GetRequiredService<IRateLimiter>());
        
        return await limiter.TryConsumeAsync();
    }
}
```

### Different Limits for Different Operations

```csharp
// Configure multiple rate limiters
services.Configure<RateLimiterOptions>("ReadOperations", options =>
{
    options.Algorithm = RateLimitingAlgorithm.TokenBucket;
    options.Capacity = 1000;
    options.RefillRate = 100;
    options.RefillPeriod = TimeSpan.FromSeconds(1);
});

services.Configure<RateLimiterOptions>("WriteOperations", options =>
{
    options.Algorithm = RateLimitingAlgorithm.FixedWindow;
    options.Capacity = 10;
    options.WindowSize = TimeSpan.FromMinutes(1);
});

// Use in your service
public class DataService
{
    private readonly IRateLimiter _readLimiter;
    private readonly IRateLimiter _writeLimiter;

    public DataService(IOptionsSnapshot<RateLimiterOptions> options)
    {
        _readLimiter = new TokenBucketStrategy(
            Options.Create(options.Get("ReadOperations")));
        _writeLimiter = new FixedWindowStrategy(
            Options.Create(options.Get("WriteOperations")));
    }

    public async Task<Data> ReadDataAsync()
    {
        if (!await _readLimiter.TryConsumeAsync())
        {
            throw new RateLimitExceededException("Read rate limit exceeded");
        }

        // Read logic here
        return new Data();
    }

    public async Task WriteDataAsync(Data data)
    {
        if (!await _writeLimiter.TryConsumeAsync())
        {
            throw new RateLimitExceededException("Write rate limit exceeded");
        }

        // Write logic here
    }
}
```

### Graceful Degradation

```csharp
public class ResilientService
{
    private readonly IRateLimiter _rateLimiter;

    public ResilientService(IRateLimiter rateLimiter)
    {
        _rateLimiter = rateLimiter;
    }

    public async Task<string> ProcessRequestAsync()
    {
        if (await _rateLimiter.TryConsumeAsync())
        {
            // Full processing
            return await PerformFullProcessingAsync();
        }

        // Check if we can wait a bit
        var waitTime = await _rateLimiter.GetWaitTimeAsync();
        if (waitTime < TimeSpan.FromMilliseconds(100))
        {
            await Task.Delay(waitTime);
            return await PerformFullProcessingAsync();
        }

        // Fallback to cached or simplified response
        return GetCachedResponse();
    }

    private async Task<string> PerformFullProcessingAsync()
    {
        // Expensive operation
        await Task.Delay(50);
        return "Full result";
    }

    private string GetCachedResponse()
    {
        return "Cached result";
    }
}
```

### Distributed Rate Limiting with Redis

```csharp
// Configure distributed cache
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

// Configure distributed rate limiting
services.Configure<RateLimiterOptions>(options =>
{
    options.Algorithm = RateLimitingAlgorithm.TokenBucket;
    options.Capacity = 1000;
    options.RefillRate = 100;
    options.RefillPeriod = TimeSpan.FromSeconds(1);
    options.DistributedCacheKey = "MyApp:GlobalRateLimit";
});

// The rate limiter will automatically use Redis for state storage
services.AddRateLimiter();
```

## Testing Your Rate Limiter

```csharp
[Test]
public async Task ShouldRespectRateLimit()
{
    // Arrange
    var options = Options.Create(new RateLimiterOptions
    {
        Algorithm = RateLimitingAlgorithm.TokenBucket,
        Capacity = 5,
        RefillRate = 1,
        RefillPeriod = TimeSpan.FromSeconds(1)
    });

    var rateLimiter = new TokenBucketStrategy(options);

    // Act & Assert
    // Should allow first 5 requests
    for (int i = 0; i < 5; i++)
    {
        Assert.IsTrue(await rateLimiter.TryConsumeAsync());
    }

    // Should deny the 6th request
    Assert.IsFalse(await rateLimiter.TryConsumeAsync());

    // Wait for refill and try again
    await Task.Delay(TimeSpan.FromSeconds(1.1));
    Assert.IsTrue(await rateLimiter.TryConsumeAsync());
}
```

## Error Handling

```csharp
public async Task<IActionResult> HandleRequestAsync()
{
    try
    {
        var waitTime = await _rateLimiter.GetWaitTimeAsync();
        
        if (waitTime > TimeSpan.Zero)
        {
            Response.Headers.Add("Retry-After", waitTime.TotalSeconds.ToString());
            return StatusCode(429, new
            {
                error = "Rate limit exceeded",
                retryAfter = waitTime.TotalSeconds
            });
        }

        if (!await _rateLimiter.TryConsumeAsync())
        {
            return StatusCode(429, "Rate limit exceeded");
        }

        // Process request
        return Ok(await ProcessRequestAsync());
    }
    catch (Exception ex)
    {
        // Log error and provide fallback
        _logger.LogError(ex, "Rate limiter error");
        return StatusCode(500, "Internal server error");
    }
}
```