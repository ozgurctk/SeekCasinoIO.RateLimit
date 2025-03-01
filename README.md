# SeekCasinoIO Rate Limiting Implementation

This project provides a flexible rate limiting solution for the SeekCasinoIO API, built according to Clean Architecture principles.

## Features

- Client-based rate limiting (by IP address or API key)
- Endpoint-specific rate limiting policies
- Multiple rate limiting strategies (fixed window, sliding window, token bucket)
- Distributed rate limiting with Redis support
- Integration with ASP.NET Core middleware pipeline
- Clean Architecture design
- Easy configuration through appsettings.json

## Architecture

The solution follows Clean Architecture principles, with clear separation of concerns:

- **Core**: Contains the domain entities, interfaces, and business rules for rate limiting
- **Infrastructure**: Implements the interfaces defined in the Core layer, including storage options
- **API**: Contains middleware and extensions for integrating rate limiting into ASP.NET Core

## Getting Started

### Installation

1. Add the project as a reference or use the NuGet package:

```bash
dotnet add package SeekCasinoIO.RateLimit
```

2. Configure rate limiting in your `Program.cs` or `Startup.cs`:

```csharp
builder.Services.AddRateLimiting(builder.Configuration);
```

3. Add the middleware to your pipeline:

```csharp
app.UseRateLimiting();
```

4. Configure rate limiting settings in your `appsettings.json`:

```json
{
  "RateLimiting": {
    "EnableRateLimiting": true,
    "DefaultRateLimit": {
      "PermitLimit": 100,
      "Window": "00:01:00",
      "QueueLimit": 0
    },
    "ClientIdHeader": "X-API-Key",
    "EndpointLimits": [
      {
        "Endpoint": "/api/casinos",
        "HttpMethod": "GET",
        "PermitLimit": 200,
        "Window": "00:01:00"
      },
      {
        "Endpoint": "/api/auth/login",
        "HttpMethod": "*",
        "PermitLimit": 20,
        "Window": "00:01:00"
      }
    ],
    "ClientRateLimits": [
      {
        "ClientId": "admin-client",
        "PermitLimit": 1000,
        "Window": "00:01:00"
      }
    ]
  }
}
```

## Usage

### Applying Rate Limits to Controllers or Actions

You can use attributes to apply specific rate limits to controllers or actions:

```csharp
[RateLimit(PermitLimit = 50, Window = 60)]
public class CasinosController : ControllerBase
{
    // This whole controller is limited to 50 requests per minute

    [RateLimit(PermitLimit = 10, Window = 60)]
    [HttpPost]
    public async Task<IActionResult> CreateCasino(CreateCasinoCommand command)
    {
        // This specific action is limited to 10 requests per minute
    }
}
```

### Programmatic Control

You can also have more programmatic control over rate limiting:

```csharp
public class SomeService
{
    private readonly IRateLimitService _rateLimitService;

    public SomeService(IRateLimitService rateLimitService)
    {
        _rateLimitService = rateLimitService;
    }

    public async Task SomeOperation(string clientId)
    {
        // Check if operation can proceed based on rate limit
        var leaseResult = await _rateLimitService.AcquireAsync(clientId, "custom-operation");
        
        if (leaseResult.IsAcquired)
        {
            // Proceed with operation
        }
        else
        {
            // Reject the operation
            throw new RateLimitExceededException("Rate limit exceeded");
        }
    }
}
```

## Configuration Options

### General Options

- `EnableRateLimiting`: Enable or disable rate limiting globally
- `DefaultRateLimit`: Default limits applied to all endpoints
- `ClientIdHeader`: Header name for client identification (default: X-API-Key)

### Endpoint-Specific Options

- `Endpoint`: The URL path pattern to apply limits to
- `HttpMethod`: HTTP method to apply limits to (* for all methods)
- `PermitLimit`: Number of requests allowed in the window
- `Window`: Time window in format "hh:mm:ss"
- `QueueLimit`: Number of requests to queue when limit is reached (0 for no queueing)

### Client-Specific Options

- `ClientId`: Identifier for the client (API key or other ID)
- `PermitLimit`: Number of requests allowed for this client
- `Window`: Time window for this client

## Storage Providers

By default, in-memory storage is used for rate limiting. For distributed scenarios, Redis is supported.

To configure Redis:

```csharp
builder.Services.AddRateLimiting(builder.Configuration, options =>
{
    options.UseRedisStorage("your-redis-connection-string");
});
```

## License

MIT