using System;

namespace SeekCasinoIO.RateLimit.Core.Exceptions;

/// <summary>
/// Exception thrown when a rate limit is exceeded.
/// </summary>
public class RateLimitExceededException : Exception
{
    /// <summary>
    /// Gets the client ID that exceeded the rate limit.
    /// </summary>
    public string ClientId { get; }

    /// <summary>
    /// Gets the resource/endpoint that was rate limited.
    /// </summary>
    public string Resource { get; }

    /// <summary>
    /// Gets the number of seconds until the rate limit resets.
    /// </summary>
    public int RetryAfter { get; }

    /// <summary>
    /// Gets the UTC timestamp when the rate limit resets.
    /// </summary>
    public DateTimeOffset ResetAt { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public RateLimitExceededException(string message) 
        : base(message)
    {
        ClientId = string.Empty;
        Resource = string.Empty;
        RetryAfter = 60;
        ResetAt = DateTimeOffset.UtcNow.AddSeconds(RetryAfter);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="clientId">The client ID that exceeded the rate limit.</param>
    /// <param name="resource">The resource/endpoint that was rate limited.</param>
    /// <param name="retryAfter">The number of seconds until the rate limit resets.</param>
    public RateLimitExceededException(string message, string clientId, string resource, int retryAfter) 
        : base(message)
    {
        ClientId = clientId;
        Resource = resource;
        RetryAfter = retryAfter;
        ResetAt = DateTimeOffset.UtcNow.AddSeconds(RetryAfter);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="clientId">The client ID that exceeded the rate limit.</param>
    /// <param name="resource">The resource/endpoint that was rate limited.</param>
    /// <param name="retryAfter">The number of seconds until the rate limit resets.</param>
    /// <param name="innerException">The inner exception.</param>
    public RateLimitExceededException(string message, string clientId, string resource, int retryAfter, Exception innerException) 
        : base(message, innerException)
    {
        ClientId = clientId;
        Resource = resource;
        RetryAfter = retryAfter;
        ResetAt = DateTimeOffset.UtcNow.AddSeconds(RetryAfter);
    }
}