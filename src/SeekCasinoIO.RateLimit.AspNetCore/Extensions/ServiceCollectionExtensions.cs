using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeekCasinoIO.RateLimit.Core.Options;
using SeekCasinoIO.RateLimit.Infrastructure.Extensions;
using System;

namespace SeekCasinoIO.RateLimit.AspNetCore.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to add rate limiting.
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
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<RateLimitOptions>? configure = null)
    {
        // Configure options from configuration and optional delegate
        services.Configure<RateLimitOptions>(configuration.GetSection("RateLimiting"));
        
        if (configure != null)
        {
            services.Configure(configure);
        }
        
        // Add infrastructure services with the default in-memory storage
        services.AddRateLimitInfrastructure(configuration, builder =>
        {
            // Check if Redis connection string is provided
            var redisConnection = configuration.GetSection("RateLimiting:RedisConnectionString").Value;
            if (!string.IsNullOrEmpty(redisConnection))
            {
                builder.UseRedisStorage(redisConnection);
            }
            else
            {
                builder.UseInMemoryStorage();
            }
        });
        
        return services;
    }
    
    /// <summary>
    /// Adds rate limiting services to the <see cref="IServiceCollection"/> with custom infrastructure configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="configureOptions">An optional action to configure rate limiting options.</param>
    /// <param name="configureInfrastructure">An action to configure the rate limiting infrastructure.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<RateLimitInfrastructureBuilder> configureInfrastructure,
        Action<RateLimitOptions>? configureOptions = null)
    {
        // Configure options from configuration and optional delegate
        services.Configure<RateLimitOptions>(configuration.GetSection("RateLimiting"));
        
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        
        // Add infrastructure services with custom configuration
        services.AddRateLimitInfrastructure(configuration, configureInfrastructure);
        
        return services;
    }
}