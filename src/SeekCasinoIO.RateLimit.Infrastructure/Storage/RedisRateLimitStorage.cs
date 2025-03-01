using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeekCasinoIO.RateLimit.Core.Interfaces;
using SeekCasinoIO.RateLimit.Core.Options;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace SeekCasinoIO.RateLimit.Infrastructure.Storage;

/// <summary>
/// Redis implementation of the rate limit storage.
/// </summary>
public class RedisRateLimitStorage : IRateLimitStorage, IDisposable
{
    private readonly ILogger<RedisRateLimitStorage> _logger;
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ConcurrentDictionary<string, Task<long>> _incrementTasks = new();
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of the <see cref="RedisRateLimitStorage"/> class.
    /// </summary>
    /// <param name="options">The rate limit options.</param>
    /// <param name="logger">The logger.</param>
    public RedisRateLimitStorage(
        IOptions<RateLimitOptions> options,
        ILogger<RedisRateLimitStorage> logger)
    {
        _logger = logger;

        var connectionString = options.Value.RedisConnectionString;
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException("Redis connection string is required for RedisRateLimitStorage", nameof(options));
        }

        _redis = ConnectionMultiplexer.Connect(connectionString);
        _db = _redis.GetDatabase();
    }

    /// <inheritdoc />
    public async Task<long> IncrementCounterAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Incrementing counter for key: {Key}", key);

        // Use an Interlocked approach to ensure we're not sending multiple parallel increments for the same key
        return await _incrementTasks.GetOrAdd(key, _ => IncrementInRedisAsync(key, expiry, cancellationToken));
    }

    private async Task<long> IncrementInRedisAsync(string key, TimeSpan expiry, CancellationToken cancellationToken)
    {
        try
        {
            var count = await _db.StringIncrementAsync(key);
            await _db.KeyExpireAsync(key, expiry);
            
            return count;
        }
        finally
        {
            // Remove the task from the dictionary after it completes
            _incrementTasks.TryRemove(key, out _);
        }
    }

    /// <inheritdoc />
    public async Task<long> GetCounterAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting counter for key: {Key}", key);
        
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? (long)value : 0;
    }

    /// <inheritdoc />
    public async Task ResetCounterAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Resetting counter for key: {Key}", key);
        
        await _db.KeyDeleteAsync(key);
    }

    /// <inheritdoc />
    public async Task ResetCountersAsync(string keyPrefix, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Resetting counters with prefix: {KeyPrefix}", keyPrefix);

        // Use Lua script to delete keys with a pattern
        var script = @"
            local keys = redis.call('KEYS', @keyPattern)
            if #keys > 0 then
                return redis.call('DEL', unpack(keys))
            end
            return 0";

        var result = await _db.ScriptEvaluateAsync(
            script,
            new { keyPattern = $"{keyPrefix}*" });
        
        _logger.LogDebug("Reset {Count} counters with prefix: {KeyPrefix}", (long)result, keyPrefix);
    }

    /// <inheritdoc />
    public async Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting TTL for key: {Key}", key);
        
        var ttl = await _db.KeyTimeToLiveAsync(key);
        return ttl;
    }

    /// <inheritdoc />
    public async Task SetAsync(string key, string value, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Setting value for key: {Key}", key);
        
        await _db.StringSetAsync(key, value, expiry);
    }

    /// <inheritdoc />
    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting value for key: {Key}", key);
        
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? (string)value : null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="RedisRateLimitStorage"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _redis?.Dispose();
        }

        _disposed = true;
    }
}