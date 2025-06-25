# Getting Started

This guide will help you get up and running with the RateLimiter library quickly.

## Installation

The RateLimiter library is available as a NuGet package. You can install it using the Package Manager Console in Visual Studio:

```powershell
Install-Package RateLimiter
```

Or using the .NET CLI:

```bash
dotnet add package RateLimiter
```

## Basic Setup

### 1. Configure Services

First, configure the rate limiter in your dependency injection container:

```csharp
using Microsoft.Extensions.DependencyInjection;
using RateLimiter.Configuration;
using RateLimiter.Extensions;

// In your Startup.cs or Program.cs
services.Configure<RateLimiterOptions>(options =>
{
    options.Algorithm = RateLimitingAlgorithm.TokenBucket;
    options.Capacity = 100;
    options.RefillRate = 10;
    options.RefillPeriod = TimeSpan.FromSeconds(1);
});

// Register the rate limiter services
services.AddRateLimiter();
```

### 2. Use in Your Application

Inject the rate limiter into your services:

```csharp
using RateLimiter.Interfaces;

public class ApiController
{
    private readonly IRateLimiter _rateLimiter;

    public ApiController(IRateLimiter rateLimiter)
    {
        _rateLimiter = rateLimiter;
    }

    public async Task<IActionResult> ProcessRequest()
    {
        // Check if we can process the request
        if (!await _rateLimiter.TryConsumeAsync())
        {
            return StatusCode(429, "Rate limit exceeded");
        }

        // Process the request
        return Ok("Request processed successfully");
    }
}
```

## Configuration Options

### Basic Configuration

```csharp
services.Configure<RateLimiterOptions>(options =>
{
    options.Algorithm = RateLimitingAlgorithm.TokenBucket;
    options.Capacity = 100;           // Maximum tokens/requests
    options.RefillRate = 10;          // Tokens added per period
    options.RefillPeriod = TimeSpan.FromSeconds(1);
});
```

### Configuration from appsettings.json

```json
{
  "RateLimiter": {
    "Algorithm": "TokenBucket",
    "Capacity": 100,
    "RefillRate": 10,
    "RefillPeriod": "00:00:01"
  }
}
```

Then in your startup code:

```csharp
services.Configure<RateLimiterOptions>(
    configuration.GetSection("RateLimiter"));
```

## Algorithm Selection

Choose the appropriate algorithm based on your needs:

### Token Bucket (Recommended)
- Allows burst traffic up to capacity
- Smooth rate limiting over time
- Good for APIs with occasional spikes

```csharp
options.Algorithm = RateLimitingAlgorithm.TokenBucket;
```

### Fixed Window
- Simple and memory efficient
- Counts requests in fixed time windows
- May allow bursts at window boundaries

```csharp
options.Algorithm = RateLimitingAlgorithm.FixedWindow;
```

### Sliding Window
- Most accurate rate limiting
- Higher memory usage
- Best for strict rate limiting requirements

```csharp
options.Algorithm = RateLimitingAlgorithm.SlidingWindow;
```

## Next Steps

- [Configuration Guide](configuration.md) - Learn about all available configuration options
- [Usage Examples](usage-examples.md) - See practical examples for different scenarios
- [Performance Considerations](performance.md) - Optimize for your specific use case
- [API Reference](../api/) - Detailed API documentation