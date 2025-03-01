using System.Collections.Generic;

namespace SeekCasinoIO.RateLimit.Core.Options;

/// <summary>
/// Options for the rate limiting middleware.
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// Gets or sets whether rate limiting is enabled.
    /// </summary>
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>
    /// Gets or sets the default rate limit applied to all endpoints if not otherwise specified.
    /// </summary>
    public RateLimitRule DefaultRateLimit { get; set; } = new RateLimitRule
    {
        PermitLimit = 100,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0
    };

    /// <summary>
    /// Gets or sets the header name used to identify clients (e.g., X-API-Key).
    /// Default is "X-API-Key".
    /// </summary>
    public string ClientIdHeader { get; set; } = "X-API-Key";

    /// <summary>
    /// Gets or sets the specific rate limits for different endpoints.
    /// </summary>
    public List<EndpointRateLimitRule> EndpointLimits { get; set; } = new();

    /// <summary>
    /// Gets or sets the specific rate limits for different clients.
    /// </summary>
    public List<ClientRateLimitRule> ClientRateLimits { get; set; } = new();

    /// <summary>
    /// Gets or sets the headers to include in the response.
    /// </summary>
    public bool IncludeRateLimitHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets the X-RateLimit-Limit header name.
    /// </summary>
    public string RateLimitLimitHeaderName { get; set; } = "X-RateLimit-Limit";

    /// <summary>
    /// Gets or sets the X-RateLimit-Remaining header name.
    /// </summary>
    public string RateLimitRemainingHeaderName { get; set; } = "X-RateLimit-Remaining";

    /// <summary>
    /// Gets or sets the X-RateLimit-Reset header name.
    /// </summary>
    public string RateLimitResetHeaderName { get; set; } = "X-RateLimit-Reset";

    /// <summary>
    /// Gets or sets the Redis connection string if using distributed rate limiting.
    /// </summary>
    public string? RedisConnectionString { get; set; }
}

/// <summary>
/// Represents a basic rate limit rule.
/// </summary>
public class RateLimitRule
{
    /// <summary>
    /// Gets or sets the number of requests permitted in the time window.
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the time window in which the limit applies.
    /// </summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the queue limit (0 for no queue).
    /// </summary>
    public int QueueLimit { get; set; } = 0;
}

/// <summary>
/// Represents a rate limit rule for a specific endpoint.
/// </summary>
public class EndpointRateLimitRule : RateLimitRule
{
    /// <summary>
    /// Gets or sets the endpoint path to apply this rule to.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP method to apply this rule to (use "*" for all methods).
    /// </summary>
    public string HttpMethod { get; set; } = "*";
}

/// <summary>
/// Represents a rate limit rule for a specific client.
/// </summary>
public class ClientRateLimitRule : RateLimitRule
{
    /// <summary>
    /// Gets or sets the client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
}