using Microsoft.AspNetCore.Mvc;
using SeekCasinoIO.RateLimit.Core.Exceptions;
using SeekCasinoIO.RateLimit.Core.Options;
using SeekCasinoIO.RateLimit.Core.Services;

namespace SeekCasinoIO.RateLimit.Samples.Api.Controllers;

/// <summary>
/// This controller demonstrates how to use the IRateLimitService directly in your code,
/// which gives you more control over the rate limiting behavior.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class IntegrationController : ControllerBase
{
    private readonly IRateLimitService _rateLimitService;

    public IntegrationController(IRateLimitService rateLimitService)
    {
        _rateLimitService = rateLimitService;
    }

    /// <summary>
    /// Custom rate limited action that integrates directly with the IRateLimitService.
    /// </summary>
    [HttpPost("process")]
    public async Task<IActionResult> ProcessCustomAction([FromBody] ProcessRequest request)
    {
        // Get client ID from header or use IP as fallback
        var clientId = GetClientId();
        
        // Define a resource identifier for this specific operation
        var resource = $"process:{request.OperationType}";
        
        // Create a custom rate limit rule based on the operation type
        var rule = new RateLimitRule
        {
            PermitLimit = request.OperationType switch
            {
                "standard" => 100,
                "premium" => 500,
                "basic" => 20,
                _ => 10 // Default for unknown operation types
            },
            Window = TimeSpan.FromMinutes(1)
        };
        
        // Try to acquire a rate limit lease
        var result = await _rateLimitService.AcquireAsync(clientId, resource, rule);
        
        if (!result.IsAcquired)
        {
            // Include rate limit information in the response headers
            Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
            Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString();
            Response.Headers["X-RateLimit-Reset"] = result.ResetAfter.ToString();
            Response.Headers["Retry-After"] = result.ResetAfter.ToString();
            
            // Return 429 Too Many Requests
            return StatusCode(429, new
            {
                status = 429,
                title = "Too Many Requests",
                detail = $"Rate limit exceeded for operation type '{request.OperationType}'. Please try again in {result.ResetAfter} seconds.",
                retryAfter = result.ResetAfter
            });
        }
        
        // Processing would happen here in a real application
        
        return Ok(new
        {
            message = $"Successfully processed {request.OperationType} operation",
            rateLimitInfo = new
            {
                remaining = result.Remaining,
                limit = result.Limit,
                resetAfter = result.ResetAfter
            }
        });
    }

    /// <summary>
    /// Demonstrates programmatic control over rate limiting with more complex logic.
    /// </summary>
    [HttpPost("custom-throttle")]
    public async Task<IActionResult> CustomThrottle([FromBody] CustomThrottleRequest request)
    {
        var clientId = GetClientId();
        
        try
        {
            // Simulate a multi-stage process with different rate limits for each stage
            
            // Stage 1: Authentication check (higher limit)
            var authResult = await _rateLimitService.AcquireAsync(
                clientId, 
                "custom-throttle:auth",
                new RateLimitRule { PermitLimit = 100, Window = TimeSpan.FromMinutes(1) });
            
            if (!authResult.IsAcquired)
            {
                throw new RateLimitExceededException(
                    "Authentication rate limit exceeded",
                    clientId,
                    "custom-throttle:auth",
                    authResult.ResetAfter);
            }
            
            // Stage 2: Processing (lower limit based on operation cost)
            int operationCost = CalculateOperationCost(request);
            
            // Different operations might have different costs against the same bucket
            var processResource = $"custom-throttle:process:{request.Priority}";
            
            for (int i = 0; i < operationCost; i++)
            {
                var processResult = await _rateLimitService.AcquireAsync(
                    clientId,
                    processResource,
                    new RateLimitRule
                    {
                        PermitLimit = request.Priority == "high" ? 50 : 20,
                        Window = TimeSpan.FromMinutes(1)
                    });
                
                if (!processResult.IsAcquired)
                {
                    // If we can't get all the "tokens" we need, reset what we've taken
                    await _rateLimitService.ResetAsync(clientId, processResource);
                    
                    throw new RateLimitExceededException(
                        $"Processing rate limit exceeded for {request.Priority} priority operation",
                        clientId,
                        processResource,
                        processResult.ResetAfter);
                }
            }
            
            // If we get here, all rate limits were satisfied
            return Ok(new { message = "Operation processed successfully" });
        }
        catch (RateLimitExceededException ex)
        {
            // Add rate limit headers
            Response.Headers["Retry-After"] = ex.RetryAfter.ToString();
            
            return StatusCode(429, new
            {
                status = 429,
                title = "Too Many Requests",
                detail = ex.Message,
                retryAfter = ex.RetryAfter
            });
        }
    }

    /// <summary>
    /// Calculates a cost for an operation based on various factors.
    /// </summary>
    private int CalculateOperationCost(CustomThrottleRequest request)
    {
        // In a real app, this might consider factors like:
        // - Size of the request
        // - Complexity of the operation
        // - Current system load
        // - User tier/subscription level
        
        return request.Priority switch
        {
            "high" => 1,
            "medium" => 2,
            "low" => 3,
            _ => 5 // Unknown priority costs more
        };
    }

    /// <summary>
    /// Gets the client identifier from headers or connection.
    /// </summary>
    private string GetClientId()
    {
        // Try to get API key from header
        if (Request.Headers.TryGetValue("X-API-Key", out var apiKey) && !string.IsNullOrEmpty(apiKey))
        {
            return apiKey!;
        }
        
        // Fallback to IP address
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

public class ProcessRequest
{
    public string OperationType { get; set; } = "standard";
}

public class CustomThrottleRequest
{
    public string Priority { get; set; } = "medium";
}