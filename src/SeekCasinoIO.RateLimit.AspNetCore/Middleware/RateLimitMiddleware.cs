using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeekCasinoIO.RateLimit.Core.Attributes;
using SeekCasinoIO.RateLimit.Core.Exceptions;
using SeekCasinoIO.RateLimit.Core.Models;
using SeekCasinoIO.RateLimit.Core.Options;
using SeekCasinoIO.RateLimit.Core.Services;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace SeekCasinoIO.RateLimit.AspNetCore.Middleware;

/// <summary>
/// Middleware for enforcing rate limits on requests.
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly IOptions<RateLimitOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The rate limit options.</param>
    /// <param name="logger">The logger.</param>
    public RateLimitMiddleware(
        RequestDelegate next,
        IOptions<RateLimitOptions> options,
        ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="rateLimitService">The rate limit service.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, IRateLimitService rateLimitService)
    {
        // If rate limiting is disabled, bypass the middleware
        if (!_options.Value.EnableRateLimiting)
        {
            await _next(context);
            return;
        }

        // Get client identifier
        var clientId = GetClientId(context);
        
        // Get resource/endpoint identifier
        var resource = GetResourceIdentifier(context);
        
        // Get rate limit attribute if present
        var rateLimitAttribute = GetRateLimitAttribute(context);
        
        try
        {
            // Handle exempt roles if needed
            if (rateLimitAttribute?.ExemptRoles != null && 
                rateLimitAttribute.ExemptRoles.Any() && 
                context.User.Identity?.IsAuthenticated == true)
            {
                // Check if user has any of the exempt roles
                var isExempt = rateLimitAttribute.ExemptRoles.Any(role => context.User.IsInRole(role));
                if (isExempt)
                {
                    _logger.LogDebug("User is exempt from rate limiting due to role");
                    await _next(context);
                    return;
                }
            }

            // Acquire rate limit lease
            RateLimitResult result;
            if (rateLimitAttribute != null)
            {
                // Use rule from attribute
                var rule = new RateLimitRule
                {
                    PermitLimit = rateLimitAttribute.PermitLimit,
                    Window = rateLimitAttribute.WindowTimeSpan,
                    QueueLimit = rateLimitAttribute.QueueLimit
                };
                
                result = await rateLimitService.AcquireAsync(
                    rateLimitAttribute.IncludeClientId ? clientId : "global", 
                    resource,
                    rule);
            }
            else
            {
                // Use default rules from options
                result = await rateLimitService.AcquireAsync(clientId, resource);
            }

            // Add rate limit headers to the response
            if (_options.Value.IncludeRateLimitHeaders)
            {
                AddRateLimitHeaders(context, result);
            }

            // Check if rate limit was exceeded
            if (!result.IsAcquired)
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId} on resource {Resource}",
                    clientId, resource);
                
                await HandleRateLimitExceeded(context, clientId, resource, result);
                return;
            }
            
            // Continue to the next middleware if rate limit was not exceeded
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying rate limiting");
            
            // Don't let rate limiting failures block the request
            await _next(context);
        }
    }

    /// <summary>
    /// Gets the client identifier from the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The client identifier.</returns>
    private string GetClientId(HttpContext context)
    {
        // Try to get client ID from the specified header
        var headerName = _options.Value.ClientIdHeader;
        if (!string.IsNullOrEmpty(headerName) && context.Request.Headers.TryGetValue(headerName, out var clientIdHeader))
        {
            return clientIdHeader.ToString();
        }
        
        // Fallback to IP address
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Gets the resource identifier from the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The resource identifier.</returns>
    private static string GetResourceIdentifier(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "/";
        var method = context.Request.Method;
        
        return $"{method}:{path}";
    }

    /// <summary>
    /// Gets the rate limit attribute from the controller or action.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The rate limit attribute, if present.</returns>
    private static RateLimitAttribute? GetRateLimitAttribute(HttpContext context)
    {
        if (context.GetEndpoint()?.Metadata?.GetMetadata<ControllerActionDescriptor>() is ControllerActionDescriptor actionDescriptor)
        {
            // First check action for the attribute
            var actionAttribute = actionDescriptor.MethodInfo.GetCustomAttribute<RateLimitAttribute>();
            if (actionAttribute != null)
            {
                return actionAttribute;
            }
            
            // Then check controller for the attribute
            return actionDescriptor.ControllerTypeInfo.GetCustomAttribute<RateLimitAttribute>();
        }
        
        return null;
    }

    /// <summary>
    /// Adds rate limit headers to the response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="result">The rate limit result.</param>
    private void AddRateLimitHeaders(HttpContext context, RateLimitResult result)
    {
        var response = context.Response;
        
        response.Headers[_options.Value.RateLimitLimitHeaderName] = result.Limit.ToString();
        response.Headers[_options.Value.RateLimitRemainingHeaderName] = result.Remaining.ToString();
        response.Headers[_options.Value.RateLimitResetHeaderName] = result.ResetAfter.ToString();
    }

    /// <summary>
    /// Handles when a rate limit is exceeded.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="resource">The resource/endpoint.</param>
    /// <param name="result">The rate limit result.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task HandleRateLimitExceeded(
        HttpContext context,
        string clientId,
        string resource,
        RateLimitResult result)
    {
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.Headers["Retry-After"] = result.ResetAfter.ToString();
        
        var error = new
        {
            status = 429,
            title = "Too Many Requests",
            detail = $"Rate limit exceeded. Please try again in {result.ResetAfter} seconds.",
            retryAfter = result.ResetAfter,
            resetAt = result.ResetAt
        };
        
        context.Response.ContentType = "application/json";
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(error));
    }
}