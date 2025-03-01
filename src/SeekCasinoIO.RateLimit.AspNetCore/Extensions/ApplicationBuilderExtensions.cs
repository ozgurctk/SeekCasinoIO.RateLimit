using Microsoft.AspNetCore.Builder;
using SeekCasinoIO.RateLimit.AspNetCore.Middleware;

namespace SeekCasinoIO.RateLimit.AspNetCore.Extensions;

/// <summary>
/// Extension methods for the <see cref="IApplicationBuilder"/> interface.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the rate limiting middleware to the pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitMiddleware>();
    }
}