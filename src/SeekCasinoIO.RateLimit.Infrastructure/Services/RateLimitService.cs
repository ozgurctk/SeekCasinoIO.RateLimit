using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeekCasinoIO.RateLimit.Core.Helpers;
using SeekCasinoIO.RateLimit.Core.Models;
using SeekCasinoIO.RateLimit.Core.Options;
using SeekCasinoIO.RateLimit.Core.Services;
using SeekCasinoIO.RateLimit.Core.Storage;

namespace SeekCasinoIO.RateLimit.Infrastructure.Services;

/// <summary>
/// Implementation of the rate limit service.
/// </summary>
public class RateLimitService : IRateLimitService
{
    private readonly IRateLimitStorage _storage;
    private readonly IOptions<RateLimitOptions> _options;
    private readonly ILogger<RateLimitService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitService"/> class.
    /// </summary>
    /// <param name="storage">The rate limit storage.</param>
    /// <param name="options">The rate limit options.</param>
    /// <param name="logger">The logger.</param>
    public RateLimitService(
        IRateLimitStorage storage,
        IOptions<RateLimitOptions> options,
        ILogger<RateLimitService> logger)
    {
        _storage = storage;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RateLimitResult> AcquireAsync(string clientId, string resource, CancellationToken cancellationToken = default)
    {
        // If rate limiting is disabled, always succeed
        if (!_options.Value.EnableRateLimiting)
        {
            return CreateAlwaysSuccessResult();
        }

        // Get the appropriate rate limit rule
        var rule = GetRateLimitRule(clientId, resource);
        
        return await AcquireAsync(clientId, resource, rule, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RateLimitResult> AcquireAsync(string clientId, string resource, RateLimitRule rule, CancellationToken cancellationToken = default)
    {
        // If rate limiting is disabled, always succeed
        if (!_options.Value.EnableRateLimiting)
        {
            return CreateAlwaysSuccessResult();
        }

        // Generate storage key
        var key = RateLimitKeyHelper.CreateKey(clientId, resource);
        
        // Try to increment the counter
        var count = await _storage.IncrementAsync(key, rule.Window, cancellationToken);
        
        // Get the remaining time until reset
        var timeToLive = await _storage.GetTimeToLiveAsync(key, cancellationToken);
        var resetAfter = (int)Math.Ceiling(timeToLive.TotalSeconds);
        var resetAt = DateTimeOffset.UtcNow.AddSeconds(resetAfter);
        
        // Check if we exceed the limit
        if (count > rule.PermitLimit)
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientId} on resource {Resource}: {Count}/{Limit}",
                clientId, resource, count, rule.PermitLimit);
            
            return RateLimitResult.Failure(0, rule.PermitLimit, resetAfter, resetAt);
        }
        
        // Calculate remaining permits
        var remaining = Math.Max(0, rule.PermitLimit - count);
        
        _logger.LogDebug("Rate limit lease acquired for client {ClientId} on resource {Resource}: {Count}/{Limit}, " +
                          "remaining: {Remaining}, reset after: {ResetAfter}s",
            clientId, resource, count, rule.PermitLimit, remaining, resetAfter);
        
        return RateLimitResult.Success(remaining, rule.PermitLimit, resetAfter, resetAt);
    }

    /// <inheritdoc />
    public async Task<RateLimitResult> GetInfoAsync(string clientId, string resource, CancellationToken cancellationToken = default)
    {
        // If rate limiting is disabled, always return a default success result
        if (!_options.Value.EnableRateLimiting)
        {
            return CreateAlwaysSuccessResult();
        }

        // Get the appropriate rate limit rule
        var rule = GetRateLimitRule(clientId, resource);
        
        // Generate storage key
        var key = RateLimitKeyHelper.CreateKey(clientId, resource);
        
        // Get the current count
        var count = await _storage.GetCountAsync(key, cancellationToken);
        
        // Get the remaining time until reset
        var timeToLive = await _storage.GetTimeToLiveAsync(key, cancellationToken);
        var resetAfter = (int)Math.Ceiling(timeToLive.TotalSeconds);
        var resetAt = DateTimeOffset.UtcNow.AddSeconds(resetAfter);
        
        // Calculate remaining permits
        var remaining = Math.Max(0, rule.PermitLimit - count);
        
        _logger.LogDebug("Rate limit info for client {ClientId} on resource {Resource}: {Count}/{Limit}, " +
                         "remaining: {Remaining}, reset after: {ResetAfter}s",
            clientId, resource, count, rule.PermitLimit, remaining, resetAfter);
        
        return RateLimitResult.Success(remaining, rule.PermitLimit, resetAfter, resetAt);
    }

    /// <inheritdoc />
    public async Task ResetAsync(string clientId, string resource, CancellationToken cancellationToken = default)
    {
        var key = RateLimitKeyHelper.CreateKey(clientId, resource);
        await _storage.ResetAsync(key, cancellationToken);
        _logger.LogDebug("Reset rate limit for client {ClientId} on resource {Resource}", clientId, resource);
    }

    /// <inheritdoc />
    public async Task ResetClientAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var pattern = RateLimitKeyHelper.CreateClientKeyPattern(clientId);
        await _storage.ResetByPatternAsync(pattern, cancellationToken);
        _logger.LogDebug("Reset all rate limits for client {ClientId}", clientId);
    }

    /// <inheritdoc />
    public async Task ResetResourceAsync(string resource, CancellationToken cancellationToken = default)
    {
        var pattern = RateLimitKeyHelper.CreateResourceKeyPattern(resource);
        await _storage.ResetByPatternAsync(pattern, cancellationToken);
        _logger.LogDebug("Reset all rate limits for resource {Resource}", resource);
    }

    /// <summary>
    /// Gets the appropriate rate limit rule for the specified client and resource.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="resource">The resource/endpoint.</param>
    /// <returns>The applicable rate limit rule.</returns>
    private RateLimitRule GetRateLimitRule(string clientId, string resource)
    {
        // First, check for client-specific limit
        var clientRule = _options.Value.ClientRateLimits.FirstOrDefault(r => r.ClientId == clientId);
        if (clientRule != null)
        {
            _logger.LogDebug("Using client-specific rate limit for {ClientId}", clientId);
            return clientRule;
        }

        // Then, check for endpoint-specific limit
        var endpointRule = _options.Value.EndpointLimits.FirstOrDefault(r => 
            r.Endpoint == resource || 
            (r.Endpoint.EndsWith("*") && resource.StartsWith(r.Endpoint.TrimEnd('*'))));
        
        if (endpointRule != null)
        {
            _logger.LogDebug("Using endpoint-specific rate limit for {Resource}", resource);
            return endpointRule;
        }

        // Finally, use default rule
        _logger.LogDebug("Using default rate limit for {ClientId} on {Resource}", clientId, resource);
        return _options.Value.DefaultRateLimit;
    }

    /// <summary>
    /// Creates a result that always indicates success when rate limiting is disabled.
    /// </summary>
    /// <returns>A success rate limit result.</returns>
    private static RateLimitResult CreateAlwaysSuccessResult()
    {
        return RateLimitResult.Success(
            remaining: int.MaxValue,
            limit: int.MaxValue,
            resetAfter: 0,
            resetAt: DateTimeOffset.MaxValue);
    }
}