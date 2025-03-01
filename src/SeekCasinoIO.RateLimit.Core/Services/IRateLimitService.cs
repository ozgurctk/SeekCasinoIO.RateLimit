using SeekCasinoIO.RateLimit.Core.Models;
using SeekCasinoIO.RateLimit.Core.Options;

namespace SeekCasinoIO.RateLimit.Core.Services;

/// <summary>
/// Provides rate limiting functionality.
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// Attempts to acquire a lease for the specified client and resource.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="resource">The resource/endpoint being accessed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A RateLimitResult indicating whether the lease was acquired.</returns>
    Task<RateLimitResult> AcquireAsync(string clientId, string resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to acquire a lease using a specific rate limit rule.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="resource">The resource/endpoint being accessed.</param>
    /// <param name="rule">The specific rate limit rule to apply.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A RateLimitResult indicating whether the lease was acquired.</returns>
    Task<RateLimitResult> AcquireAsync(string clientId, string resource, RateLimitRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about the current rate limit status without acquiring a lease.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="resource">The resource/endpoint.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A RateLimitResult with the current limit information.</returns>
    Task<RateLimitResult> GetInfoAsync(string clientId, string resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the rate limit for the specified client and resource.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="resource">The resource/endpoint.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResetAsync(string clientId, string resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all rate limits for the specified client.
    /// </summary>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResetClientAsync(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all rate limits for all clients accessing the specified resource.
    /// </summary>
    /// <param name="resource">The resource/endpoint.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResetResourceAsync(string resource, CancellationToken cancellationToken = default);
}