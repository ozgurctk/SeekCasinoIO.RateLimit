using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SeekCasinoIO.RateLimit.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace SeekCasinoIO.RateLimit.Samples.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;
    
    // Simple in-memory user store for demo purposes
    private static readonly List<User> _users = new()
    {
        new User { Id = 1, Username = "admin", Password = "admin123", Role = "Admin" },
        new User { Id = 2, Username = "customer", Password = "customer123", Role = "Customer" }
    };
    
    public AuthController(ILogger<AuthController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }
    
    [HttpPost("login")]
    [RateLimit(PermitLimit = 5, Window = 60, ExemptRoles = new[] { "Admin" })]
    public IActionResult Login(LoginRequest request)
    {
        _logger.LogInformation("Login attempt for user: {Username}", request.Username);
        
        // Validate credentials
        var user = _users.Find(u => u.Username == request.Username && u.Password == request.Password);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }
        
        // Generate JWT token
        var token = GenerateJwtToken(user);
        
        return Ok(new { token });
    }
    
    [HttpPost("register")]
    [RateLimit(PermitLimit = 2, Window = 60)]
    public IActionResult Register(RegisterRequest request)
    {
        _logger.LogInformation("Registration attempt for user: {Username}", request.Username);
        
        // Check if username is already taken
        if (_users.Exists(u => u.Username == request.Username))
        {
            return BadRequest(new { message = "Username is already taken" });
        }
        
        // Create new user
        var user = new User
        {
            Id = _users.Count + 1,
            Username = request.Username,
            Password = request.Password,
            Role = "Customer" // Default role
        };
        
        _users.Add(user);
        
        // Generate JWT token
        var token = GenerateJwtToken(user);
        
        return Ok(new { token });
    }
    
    private string GenerateJwtToken(User user)
    {
        // Create claims for the token
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role)
        };
        
        // Create key and credentials
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecretKey123!ThisIsADemoKeyForSampleAppOnly"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        // Create token
        var token = new JwtSecurityToken(
            issuer: "SeekCasinoIO.RateLimit.Sample",
            audience: "SeekCasinoIO.RateLimit.Users",
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds);
            
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}