using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SeekCasinoIO.RateLimit.Core.Options;
using SeekCasinoIO.RateLimit.Core.Services;
using SeekCasinoIO.RateLimit.Core.Storage;
using SeekCasinoIO.RateLimit.Infrastructure.Services;
using SeekCasinoIO.RateLimit.Infrastructure.Storage;
using StackExchange.Redis;
using System;

namespace SeekCasinoIO.RateLimit.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring rate limiting in the infrastructure layer.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds rate limiting services to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="configure">An optional action to configure rate limiting options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddRateLimitInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<RateLimitInfrastructureBuilder>? configure = null)
    {
        // Add options from configuration
        services.Configure<RateLimitOptions>(configuration.GetSection("RateLimiting"));

        // Create the builder
        var builder = new RateLimitInfrastructureBuilder(services);

        // Use in-memory storage by default
        builder.UseInMemoryStorage();

        // Apply custom configuration if provided
        configure?.Invoke(builder);

        // Add the rate limiting service
        services.AddScoped<IRateLimitService, RateLimitService>();

        return services;
    }
}

/// <summary>
/// Builder for configuring rate limiting infrastructure.
/// </summary>
public class RateLimitInfrastructureBuilder
{
    private readonly IServiceCollection _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitInfrastructureBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public RateLimitInfrastructureBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Configures the rate limiter to use in-memory storage.
    /// </summary>
    /// <returns>The builder.</returns>
    public RateLimitInfrastructureBuilder UseInMemoryStorage()
    {
        _services.AddMemoryCache();
        _services.AddSingleton<IRateLimitStorage, InMemoryRateLimitStorage>();
        return this;
    }

    /// <summary>
    /// Configures the rate limiter to use Redis storage.
    /// </summary>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <returns>The builder.</returns>
    public RateLimitInfrastructureBuilder UseRedisStorage(string connectionString)
    {
        _services.AddSingleton<IConnectionMultiplexer>(sp => 
            ConnectionMultiplexer.Connect(connectionString));
            
        _services.AddSingleton<IRateLimitStorage, RedisRateLimitStorage>();
        
        // Update the options to include the Redis connection string
        _services.PostConfigure<RateLimitOptions>(options => 
        {
            options.RedisConnectionString = connectionString;
        });
        
        return this;
    }

    /// <summary>
    /// Configures the rate limiter to use Redis storage from the options.
    /// </summary>
    /// <returns>The builder.</returns>
    public RateLimitInfrastructureBuilder UseRedisStorageFromOptions()
    {
        _services.AddSingleton<IConnectionMultiplexer>(sp => 
        {
            var options = sp.GetRequiredService<IOptions<RateLimitOptions>>().Value;
            
            if (string.IsNullOrEmpty(options.RedisConnectionString))
            {
                throw new InvalidOperationException(
                    "Redis connection string is not configured. Add RedisConnectionString to RateLimiting section in configuration.");
            }
            
            return ConnectionMultiplexer.Connect(options.RedisConnectionString);
        });
        
        _services.AddSingleton<IRateLimitStorage, RedisRateLimitStorage>();
        
        return this;
    }
}