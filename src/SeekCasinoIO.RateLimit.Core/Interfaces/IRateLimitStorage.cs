namespace SeekCasinoIO.RateLimit.Core.Interfaces;

/// <summary>
/// Storage interface for rate limiting data.
/// </summary>
public interface IRateLimitStorage
{
    /// <summary>
    /// Increments a counter and returns the current count.
    /// </summary>
    /// <param name="key">The key for the counter.</param>
    /// <param name="expiry">The expiry time for the counter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current count after incrementing.</returns>
    Task<long> IncrementCounterAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current count for a counter.
    /// </summary>
    /// <param name="key">The key for the counter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current count, or 0 if the key does not exist.</returns>
    Task<long> GetCounterAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a counter to zero.
    /// </summary>
    /// <param name="key">The key for the counter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResetCounterAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all counters for a specific prefix.
    /// </summary>
    /// <param name="keyPrefix">The prefix for the counters to reset.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResetCountersAsync(string keyPrefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the time to live for a counter.
    /// </summary>
    /// <param name="key">The key for the counter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The remaining time to live, or null if the key does not exist.</returns>
    Task<TimeSpan?> GetTimeToLiveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value with an expiry.
    /// </summary>
    /// <param name="key">The key for the value.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="expiry">The expiry time for the value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetAsync(string key, string value, TimeSpan expiry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value.
    /// </summary>
    /// <param name="key">The key for the value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The value, or null if the key does not exist.</returns>
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);
}