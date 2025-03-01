namespace SeekCasinoIO.RateLimit.Core.Exceptions;

/// <summary>
/// Exception thrown when a rate limit is exceeded.
/// </summary>
public class RateLimitExceededException : Exception
{
    /// <summary>
    /// Gets the number of seconds until the rate limit resets.
    /// </summary>
    public int RetryAfter { get; }

    /// <summary>
    /// Gets the UTC time when the rate limit resets.
    /// </summary>
    public DateTimeOffset RetryAt { get; }

    /// <summary>
    /// Gets the client ID that exceeded the rate limit.
    /// </summary>
    public string? ClientId { get; }

    /// <summary>
    /// Gets the resource that was rate limited.
    /// </summary>
    public string? Resource { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="RateLimitExceededException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public RateLimitExceededException(string message) : base(message)
    {
        RetryAfter = 60; // Default to 1 minute
        RetryAt = DateTimeOffset.UtcNow.AddSeconds(RetryAfter);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="RateLimitExceededException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="retryAfter">The number of seconds until retry is allowed.</param>
    public RateLimitExceededException(string message, int retryAfter) : base(message)
    {
        RetryAfter = retryAfter;
        RetryAt = DateTimeOffset.UtcNow.AddSeconds(RetryAfter);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="RateLimitExceededException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="retryAfter">The number of seconds until retry is allowed.</param>
    /// <param name="clientId">The client ID that exceeded the rate limit.</param>
    /// <param name="resource">The resource that was rate limited.</param>
    public RateLimitExceededException(string message, int retryAfter, string? clientId, string? resource) : base(message)
    {
        RetryAfter = retryAfter;
        RetryAt = DateTimeOffset.UtcNow.AddSeconds(RetryAfter);
        ClientId = clientId;
        Resource = resource;
    }
}