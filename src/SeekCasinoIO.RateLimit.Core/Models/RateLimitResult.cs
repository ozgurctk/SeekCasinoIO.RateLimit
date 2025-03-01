namespace SeekCasinoIO.RateLimit.Core.Models;

/// <summary>
/// Represents the result of a rate limit lease acquisition.
/// </summary>
public class RateLimitResult
{
    /// <summary>
    /// Gets whether the lease was acquired.
    /// </summary>
    public bool IsAcquired { get; private set; }

    /// <summary>
    /// Gets the number of requests remaining in the time window.
    /// </summary>
    public int Remaining { get; private set; }

    /// <summary>
    /// Gets the total number of requests permitted in the time window.
    /// </summary>
    public int Limit { get; private set; }

    /// <summary>
    /// Gets the number of seconds until the rate limit resets.
    /// </summary>
    public int ResetAfter { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the rate limit resets.
    /// </summary>
    public DateTimeOffset ResetAt { get; private set; }

    /// <summary>
    /// Gets or sets any additional metadata associated with the rate limit.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Creates a new instance of the <see cref="RateLimitResult"/> class where the lease was acquired.
    /// </summary>
    /// <param name="remaining">The number of requests remaining.</param>
    /// <param name="limit">The total request limit.</param>
    /// <param name="resetAfter">The number of seconds until reset.</param>
    /// <param name="resetAt">The UTC timestamp when the rate limit resets.</param>
    /// <returns>A successful RateLimitResult.</returns>
    public static RateLimitResult Success(int remaining, int limit, int resetAfter, DateTimeOffset resetAt)
    {
        return new RateLimitResult
        {
            IsAcquired = true,
            Remaining = remaining,
            Limit = limit,
            ResetAfter = resetAfter,
            ResetAt = resetAt
        };
    }

    /// <summary>
    /// Creates a new instance of the <see cref="RateLimitResult"/> class where the lease was not acquired.
    /// </summary>
    /// <param name="remaining">The number of requests remaining (usually 0).</param>
    /// <param name="limit">The total request limit.</param>
    /// <param name="resetAfter">The number of seconds until reset.</param>
    /// <param name="resetAt">The UTC timestamp when the rate limit resets.</param>
    /// <returns>A failed RateLimitResult.</returns>
    public static RateLimitResult Failure(int remaining, int limit, int resetAfter, DateTimeOffset resetAt)
    {
        return new RateLimitResult
        {
            IsAcquired = false,
            Remaining = remaining,
            Limit = limit,
            ResetAfter = resetAfter,
            ResetAt = resetAt
        };
    }
}