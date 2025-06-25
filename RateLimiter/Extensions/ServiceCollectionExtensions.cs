using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using RateLimiter.Adapters;
using RateLimiter.Configuration;
using RateLimiter.Services;

namespace RateLimiter.Extensions;

/// <summary>
/// Extension methods for setting up rate limiter services in DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds rate limiter services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration section containing rate limiter options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRateLimiter(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.Configure<RateLimiterOptions>(
            configuration.GetSection(RateLimiterOptions.SectionName));

        // Add factory
        services.AddSingleton<RateLimiterFactory>();

        return services;
    }

    /// <summary>
    /// Adds rate limiter services with a specific strategy as the default.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration section containing rate limiter options</param>
    /// <param name="defaultStrategy">The default strategy type to use</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRateLimiter(
        this IServiceCollection services, 
        IConfiguration configuration,
        RateLimiterStrategyType defaultStrategy)
    {
        services.AddRateLimiter(configuration);

        // Add a default rate limiter context with the specified strategy
        services.AddScoped(provider =>
        {
            var factory = provider.GetRequiredService<RateLimiterFactory>();
            var strategy = factory.CreateStrategy(defaultStrategy);
            return new RateLimiterContext(strategy);
        });

        return services;
    }

    /// <summary>
    /// Adds rate limiter services with adapter support for legacy code integration.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration section containing rate limiter options</param>
    /// <param name="defaultStrategy">The default strategy type to use</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRateLimiterWithAdapter(
        this IServiceCollection services,
        IConfiguration configuration,
        RateLimiterStrategyType defaultStrategy = RateLimiterStrategyType.TokenBucket)
    {
        services.AddRateLimiter(configuration);

        // Add adapter services
        services.AddScoped<IRateLimiterAdapter>(provider =>
        {
            var factory = provider.GetRequiredService<RateLimiterFactory>();
            var strategy = factory.CreateStrategy(defaultStrategy);
            return new UniversalRateLimiterAdapter(strategy);
        });

        // Add specific token bucket adapter for backward compatibility
        services.AddScoped<TokenBucketAdapter>();

        return services;
    }
} 