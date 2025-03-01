using SeekCasinoIO.RateLimit.Core.Models;
using SeekCasinoIO.RateLimit.Core.Options;

namespace SeekCasinoIO.RateLimit.Core.Interfaces;

/// <summary>
/// Service for rate limiting operations.
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// Acquires a rate limit lease for the specified client and resource.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="resource">The resource being rate limited.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating whether the lease was acquired and related information.</returns>
    Task<RateLimitResult> AcquireAsync(string clientId, string resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquires a rate limit lease for the specified client and resource with a custom rate limit rule.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="resource">The resource being rate limited.</param>
    /// <param name="rule">The custom rate limit rule to apply.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating whether the lease was acquired and related information.</returns>
    Task<RateLimitResult> AcquireAsync(string clientId, string resource, RateLimitRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about the current rate limit status for a client and resource.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="resource">The resource being rate limited.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Information about the current rate limit status.</returns>
    Task<RateLimitResult> GetInfoAsync(string clientId, string resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the rate limit for a client and resource.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="resource">The resource being rate limited.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResetAsync(string clientId, string resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all rate limits for a client.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResetAllAsync(string clientId, CancellationToken cancellationToken = default);
}