using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RateLimiter.Extensions;
using RateLimiter.Services;

Console.WriteLine("RateLimiter Demo - Strategy Pattern");
Console.WriteLine("==================================");

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Setup dependency injection with rate limiter services
var services = new ServiceCollection();
services.AddRateLimiter(configuration, RateLimiterStrategyType.TokenBucket);

var serviceProvider = services.BuildServiceProvider();

// Get factory to create different strategies
var factory = serviceProvider.GetRequiredService<RateLimiterFactory>();

Console.WriteLine("Testing different rate limiting strategies:");
Console.WriteLine();

// Test Token Bucket
Console.WriteLine("1. Token Bucket Strategy:");
await TestStrategy(factory.CreateTokenBucket(), "Token Bucket");

Console.WriteLine();

// Test Fixed Window
Console.WriteLine("2. Fixed Window Strategy:");
await TestStrategy(factory.CreateFixedWindow(), "Fixed Window");

Console.WriteLine();

// Test Sliding Window
Console.WriteLine("3. Sliding Window Strategy:");
await TestStrategy(factory.CreateSlidingWindow(), "Sliding Window");

Console.WriteLine();

// Test Strategy Pattern with Context
Console.WriteLine("4. Strategy Pattern with Context:");
var context = serviceProvider.GetRequiredService<RateLimiterContext>();
Console.WriteLine($"Current strategy: {context.CurrentStrategyName}");

// Try some requests
for (int i = 1; i <= 5; i++)
{
    var allowed = await context.TryConsumeAsync();
    var available = await context.GetAvailablePermitsAsync();
    Console.WriteLine($"Request {i}: {(allowed ? "✓ Allowed" : "✗ Denied")} - Available: {available}");
}

// Switch strategy at runtime
Console.WriteLine("\nSwitching to Fixed Window strategy...");
context.SetStrategy(factory.CreateFixedWindow());
Console.WriteLine($"New strategy: {context.CurrentStrategyName}");

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();

static async Task TestStrategy(RateLimiter.Interfaces.IRateLimiter rateLimiter, string strategyName)
{
    try
    {
        Console.WriteLine($"Testing {strategyName}...");
        
        for (int i = 1; i <= 3; i++)
        {
            var allowed = await rateLimiter.TryConsumeAsync();
            var available = await rateLimiter.GetAvailablePermitsAsync();
            Console.WriteLine($"  Request {i}: {(allowed ? "✓ Allowed" : "✗ Denied")} - Available: {available}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Error testing {strategyName}: {ex.Message}");
    }
} 