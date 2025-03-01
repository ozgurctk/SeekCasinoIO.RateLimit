using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeekCasinoIO.RateLimit.Core.Interfaces;
using SeekCasinoIO.RateLimit.Core.Options;
using System.Collections.Concurrent;

namespace SeekCasinoIO.RateLimit.Infrastructure.Storage;

/// <summary>
/// In-memory implementation of the rate limit storage.
/// </summary>
public class InMemoryRateLimitStorage : IRateLimitStorage
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemoryRateLimitStorage> _logger;
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _expirations = new();

    /// <summary>
    /// Creates a new instance of the <see cref="InMemoryRateLimitStorage"/> class.
    /// </summary>
    /// <param name="cache">The memory cache.</param>
    /// <param name="options">The rate limit options.</param>
    /// <param name="logger">The logger.</param>
    public InMemoryRateLimitStorage(
        IMemoryCache cache,
        IOptions<RateLimitOptions> options,
        ILogger<InMemoryRateLimitStorage> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<long> IncrementCounterAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Incrementing counter for key: {Key}", key);
        
        // Increment the counter
        var count = _counters.AddOrUpdate(
            key,
            _ => 1,
            (_, existingCount) => existingCount + 1);

        // Update the expiration time
        _expirations[key] = DateTimeOffset.UtcNow.Add(expiry);

        // Set up cache expiration to clean up the counter
        _cache.Set(
            key,
            count,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            }
            .RegisterPostEvictionCallback((_, _, _, _) =>
            {
                _counters.TryRemove(key, out _);
                _expirations.TryRemove(key, out _);
            }));

        return Task.FromResult(count);
    }

    /// <inheritdoc />
    public Task<long> GetCounterAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting counter for key: {Key}", key);
        
        // Return the current count or 0 if it doesn't exist
        return Task.FromResult(_counters.GetValueOrDefault(key));
    }

    /// <inheritdoc />
    public Task ResetCounterAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Resetting counter for key: {Key}", key);
        
        // Remove the counter
        _counters.TryRemove(key, out _);
        _expirations.TryRemove(key, out _);
        _cache.Remove(key);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ResetCountersAsync(string keyPrefix, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Resetting counters with prefix: {KeyPrefix}", keyPrefix);
        
        // Find and remove all counters with the specified prefix
        var keysToRemove = _counters.Keys
            .Where(k => k.StartsWith(keyPrefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _counters.TryRemove(key, out _);
            _expirations.TryRemove(key, out _);
            _cache.Remove(key);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting TTL for key: {Key}", key);
        
        // Check if the key exists and has an expiration
        if (_expirations.TryGetValue(key, out var expiration))
        {
            var ttl = expiration - DateTimeOffset.UtcNow;
            return Task.FromResult<TimeSpan?>(ttl > TimeSpan.Zero ? ttl : TimeSpan.Zero);
        }

        return Task.FromResult<TimeSpan?>(null);
    }

    /// <inheritdoc />
    public Task SetAsync(string key, string value, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Setting value for key: {Key}", key);
        
        _cache.Set(
            key,
            value,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting value for key: {Key}", key);
        
        if (_cache.TryGetValue(key, out string? value))
        {
            return Task.FromResult(value);
        }

        return Task.FromResult<string?>(null);
    }
}