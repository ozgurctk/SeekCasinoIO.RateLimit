using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeekCasinoIO.RateLimit.Core.Storage;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace SeekCasinoIO.RateLimit.Infrastructure.Storage;

/// <summary>
/// In-memory implementation of the rate limit storage.
/// </summary>
public class InMemoryRateLimitStorage : IRateLimitStorage
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemoryRateLimitStorage> _logger;
    private readonly ConcurrentDictionary<string, object> _locks = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryRateLimitStorage"/> class.
    /// </summary>
    /// <param name="cache">The memory cache.</param>
    /// <param name="logger">The logger.</param>
    public InMemoryRateLimitStorage(
        IMemoryCache cache,
        ILogger<InMemoryRateLimitStorage> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<long> IncrementAsync(string key, TimeSpan expiryTime, CancellationToken cancellationToken = default)
    {
        // Get a lock for the specific key to ensure atomicity
        var lockObj = _locks.GetOrAdd(key, _ => new object());

        long count;
        lock (lockObj)
        {
            // Get current count
            _cache.TryGetValue<CounterEntry>(key, out var entry);
            entry ??= new CounterEntry { Count = 0, ExpiryTime = DateTimeOffset.UtcNow.Add(expiryTime) };

            // Increment counter
            entry.Count++;
            count = entry.Count;

            // Set with absolute expiration
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = entry.ExpiryTime
            };

            _cache.Set(key, entry, cacheOptions);
        }

        _logger.LogDebug("Incremented counter for key {Key}: {Count}", key, count);
        return Task.FromResult(count);
    }

    /// <inheritdoc />
    public Task<long> GetCountAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue<CounterEntry>(key, out var entry))
        {
            return Task.FromResult(entry.Count);
        }

        return Task.FromResult(0L);
    }

    /// <inheritdoc />
    public Task<TimeSpan> GetTimeToLiveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue<CounterEntry>(key, out var entry))
        {
            var remaining = entry.ExpiryTime - DateTimeOffset.UtcNow;
            return Task.FromResult(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
        }

        return Task.FromResult(TimeSpan.Zero);
    }

    /// <inheritdoc />
    public Task<bool> ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        _locks.TryRemove(key, out _);
        _logger.LogDebug("Reset counter for key {Key}", key);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<long> ResetByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Convert Redis-style pattern to a regular expression
        var regex = PatternToRegex(pattern);
        var keys = _locks.Keys.Where(k => regex.IsMatch(k)).ToList();

        foreach (var key in keys)
        {
            _cache.Remove(key);
            _locks.TryRemove(key, out _);
        }

        _logger.LogDebug("Reset {Count} counters matching pattern {Pattern}", keys.Count, pattern);
        return Task.FromResult((long)keys.Count);
    }

    /// <summary>
    /// Converts a Redis-style pattern to a regular expression.
    /// </summary>
    private static Regex PatternToRegex(string pattern)
    {
        string regexPattern = pattern
            .Replace(".", "\\.")
            .Replace("*", ".*");

        return new Regex($"^{regexPattern}$", RegexOptions.Compiled);
    }

    /// <summary>
    /// Internal class used to store counter information.
    /// </summary>
    private class CounterEntry
    {
        public long Count { get; set; }
        public DateTimeOffset ExpiryTime { get; set; }
    }
}