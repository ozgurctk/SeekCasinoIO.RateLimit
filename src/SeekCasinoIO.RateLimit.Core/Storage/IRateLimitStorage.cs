namespace SeekCasinoIO.RateLimit.Core.Storage;

/// <summary>
/// Provides storage functionality for rate limiting data.
/// </summary>
public interface IRateLimitStorage
{
    /// <summary>
    /// Increments the counter for the specified key and returns the updated count.
    /// </summary>
    /// <param name="key">The rate limit key.</param>
    /// <param name="expiryTime">The time when the counter should expire.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The current count after incrementing.</returns>
    Task<long> IncrementAsync(string key, TimeSpan expiryTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current count for the specified key.
    /// </summary>
    /// <param name="key">The rate limit key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The current count, or 0 if the key does not exist.</returns>
    Task<long> GetCountAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the time remaining until the counter expires.
    /// </summary>
    /// <param name="key">The rate limit key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The time remaining until expiry, or TimeSpan.Zero if the key does not exist.</returns>
    Task<TimeSpan> GetTimeToLiveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the counter for the specified key.
    /// </summary>
    /// <param name="key">The rate limit key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the key was removed, false otherwise.</returns>
    Task<bool> ResetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all counters for keys that match the specified pattern.
    /// </summary>
    /// <param name="pattern">The key pattern to match (e.g., "client:*" or "*:resource").</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of keys that were removed.</returns>
    Task<long> ResetByPatternAsync(string pattern, CancellationToken cancellationToken = default);
}