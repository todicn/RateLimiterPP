# RateLimiter Library Documentation

Welcome to the comprehensive documentation for the RateLimiter library - a high-performance, thread-safe rate limiting solution for .NET applications.

## Overview

The RateLimiter library provides multiple rate limiting algorithms implemented using the Strategy and Adapter design patterns. It's designed for Shopify pair programming interviews and demonstrates professional-grade software architecture and engineering practices.

## Key Features

- **Multiple Algorithms**: Token Bucket, Fixed Window, and Sliding Window rate limiting strategies
- **Thread-Safe**: All implementations are designed for concurrent access
- **Dependency Injection**: Full support for Microsoft.Extensions.DependencyInjection
- **Configuration**: Flexible configuration using the Options pattern
- **Async/Await**: Modern async programming patterns throughout
- **High Performance**: Optimized for minimal memory allocations and high throughput

## Supported Rate Limiting Algorithms

### Token Bucket
Allows burst traffic up to a configured capacity while maintaining a steady refill rate. Ideal for scenarios where occasional bursts are acceptable.

### Fixed Window
Counts requests within fixed time windows. Simple and memory-efficient, but can allow burst traffic at window boundaries.

### Sliding Window
Provides smooth rate limiting by maintaining a rolling window of request timestamps. More accurate than fixed window but uses more memory.

## Quick Start

```csharp
// Basic usage with dependency injection
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
        if (await _rateLimiter.TryConsumeAsync())
        {
            // Process the request
            return true;
        }
        
        // Rate limit exceeded
        return false;
    }
}
```

## Architecture

The library is built using several design patterns:

- **Strategy Pattern**: Allows switching between rate limiting algorithms at runtime
- **Adapter Pattern**: Provides backward compatibility with legacy interfaces
- **Factory Pattern**: Creates appropriate rate limiter instances based on configuration
- **Options Pattern**: Manages configuration through IOptions<T>

## Getting Started

1. [Installation and Setup](articles/getting-started.md) - Learn how to install and configure the library
2. [Configuration Guide](articles/configuration.md) - Understand all configuration options
3. [Usage Examples](articles/usage-examples.md) - See practical examples for different scenarios
4. [Performance Considerations](articles/performance.md) - Optimize for your use case
5. [API Reference](api/) - Detailed API documentation

## Contributing

This project demonstrates best practices for:
- Clean architecture and SOLID principles
- Comprehensive unit testing with high coverage
- Thread-safe concurrent programming
- Modern C# and .NET features
- Professional documentation

## Support

For questions, issues, or contributions, please refer to the project repository and documentation.