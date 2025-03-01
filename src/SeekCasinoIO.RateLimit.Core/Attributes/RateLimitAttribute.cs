namespace SeekCasinoIO.RateLimit.Core.Attributes;

/// <summary>
/// Attribute to apply rate limiting to a controller or action.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RateLimitAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the number of requests permitted in the time window.
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the time window in seconds in which the limit applies.
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
    /// Gets or sets whether this rate limit applies to authenticated users only.
    /// </summary>
    public bool AuthenticatedOnly { get; set; } = false;

    /// <summary>
    /// Gets or sets whether this rate limit applies to anonymous users only.
    /// </summary>
    public bool AnonymousOnly { get; set; } = false;

    /// <summary>
    /// Gets or sets the client type to apply this limit to (e.g., "admin").
    /// If null or empty, applies to all client types.
    /// </summary>
    public string? ClientType { get; set; }

    /// <summary>
    /// Gets or sets a custom resource name for this rate limit.
    /// If null or empty, a resource name will be generated based on the route.
    /// </summary>
    public string? ResourceName { get; set; }

    /// <summary>
    /// Creates a new instance of the <see cref="RateLimitAttribute"/> class.
    /// </summary>
    public RateLimitAttribute()
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="RateLimitAttribute"/> class.
    /// </summary>
    /// <param name="permitLimit">The number of requests permitted in the time window.</param>
    /// <param name="window">The time window in seconds in which the limit applies.</param>
    public RateLimitAttribute(int permitLimit, int window)
    {
        PermitLimit = permitLimit;
        Window = window;
    }
}