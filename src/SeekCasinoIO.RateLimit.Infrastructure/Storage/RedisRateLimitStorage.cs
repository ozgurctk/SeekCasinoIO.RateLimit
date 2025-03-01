using Microsoft.Extensions.Logging;
using SeekCasinoIO.RateLimit.Core.Storage;
using StackExchange.Redis;

namespace SeekCasinoIO.RateLimit.Infrastructure.Storage;

/// <summary>
/// Redis implementation of the rate limit storage for distributed scenarios.
/// </summary>
public class RedisRateLimitStorage : IRateLimitStorage
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisRateLimitStorage> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisRateLimitStorage"/> class.
    /// </summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="logger">The logger.</param>
    public RedisRateLimitStorage(
        IConnectionMultiplexer redis,
        ILogger<RedisRateLimitStorage> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<long> IncrementAsync(string key, TimeSpan expiryTime, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        
        // INCR the key and set expiration in a transaction
        long count = await db.StringIncrementAsync(key);
        
        // Only set expiry if the key is new (count == 1)
        if (count == 1)
        {
            await db.KeyExpireAsync(key, expiryTime);
        }

        _logger.LogDebug("Incremented counter for key {Key}: {Count}", key, count);
        return count;
    }

    /// <inheritdoc />
    public async Task<long> GetCountAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(key);
        
        if (value.HasValue && value.TryParse(out long count))
        {
            return count;
        }
        
        return 0;
    }

    /// <inheritdoc />
    public async Task<TimeSpan> GetTimeToLiveAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var ttl = await db.KeyTimeToLiveAsync(key);
        
        return ttl ?? TimeSpan.Zero;
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        bool result = await db.KeyDeleteAsync(key);
        
        _logger.LogDebug("Reset counter for key {Key}: {Result}", key, result);
        return result;
    }

    /// <inheritdoc />
    public async Task<long> ResetByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var server = GetServer();
        if (server == null)
        {
            _logger.LogWarning("Could not find a Redis server to execute SCAN operation");
            return 0;
        }

        var db = _redis.GetDatabase();
        var keyCount = 0L;
        
        // Find and delete all keys matching the pattern
        foreach (var key in server.Keys(pattern: pattern))
        {
            if (await db.KeyDeleteAsync(key))
            {
                keyCount++;
            }
        }
        
        _logger.LogDebug("Reset {Count} counters matching pattern {Pattern}", keyCount, pattern);
        return keyCount;
    }

    /// <summary>
    /// Gets a Redis server for operations that require server-side commands.
    /// </summary>
    /// <returns>A server endpoint or null if none are available.</returns>
    private IServer? GetServer()
    {
        var endpoints = _redis.GetEndPoints();
        if (endpoints.Length == 0)
        {
            return null;
        }

        return _redis.GetServer(endpoints[0]);
    }
}