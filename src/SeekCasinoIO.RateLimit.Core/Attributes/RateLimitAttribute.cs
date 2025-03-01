namespace SeekCasinoIO.RateLimit.Core.Attributes;

/// <summary>
/// Attribute that specifies rate limits for a controller or action.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RateLimitAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the number of requests permitted in the time window.
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the time window in seconds.
    /// </summary>
    public int Window { get; set; } = 60;

    /// <summary>
    /// Gets or sets the queue limit (0 for no queue).
    /// </summary>
    public int QueueLimit { get; set; } = 0;

    /// <summary>
    /// Gets the time window as a TimeSpan.
    /// </summary>
    public TimeSpan WindowTimeSpan => TimeSpan.FromSeconds(Window);

    /// <summary>
    /// Gets or sets whether to include the client ID in the rate limit key.
    /// If false, all clients share the same limit for the resource.
    /// </summary>
    public bool IncludeClientId { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to exempt clients with specific roles from rate limiting.
    /// </summary>
    public string[]? ExemptRoles { get; set; }

    /// <summary>
    /// Gets or sets whether to include HTTP method in the resource identifier.
    /// If true, different HTTP methods (GET, POST, etc.) have separate limits.
    /// </summary>
    public bool IncludeHttpMethod { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitAttribute"/> class.
    /// </summary>
    public RateLimitAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitAttribute"/> class.
    /// </summary>
    /// <param name="permitLimit">The number of requests permitted in the time window.</param>
    /// <param name="window">The time window in seconds.</param>
    public RateLimitAttribute(int permitLimit, int window)
    {
        PermitLimit = permitLimit;
        Window = window;
    }
}