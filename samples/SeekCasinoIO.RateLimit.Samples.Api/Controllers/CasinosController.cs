using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SeekCasinoIO.RateLimit.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SeekCasinoIO.RateLimit.Samples.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[RateLimit(PermitLimit = 50, Window = 60)] // 50 requests per minute for the entire controller
public class CasinosController : ControllerBase
{
    private readonly ILogger<CasinosController> _logger;
    private static readonly List<CasinoModel> _casinos = new()
    {
        new CasinoModel { Id = Guid.NewGuid(), Name = "Golden Palace", Rating = 4.5m, Location = "Las Vegas" },
        new CasinoModel { Id = Guid.NewGuid(), Name = "Royal Flush", Rating = 4.2m, Location = "Atlantic City" },
        new CasinoModel { Id = Guid.NewGuid(), Name = "Diamond Club", Rating = 4.8m, Location = "Macau" },
        new CasinoModel { Id = Guid.NewGuid(), Name = "Lucky Strike", Rating = 3.9m, Location = "Monte Carlo" },
        new CasinoModel { Id = Guid.NewGuid(), Name = "Silver Star", Rating = 4.1m, Location = "Singapore" }
    };

    public CasinosController(ILogger<CasinosController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        _logger.LogInformation("Getting all casinos");
        return Ok(_casinos);
    }

    [HttpGet("{id}")]
    [RateLimit(PermitLimit = 100, Window = 60)] // More permissive rate limit for specific casino lookup
    public IActionResult GetById(Guid id)
    {
        _logger.LogInformation("Getting casino by ID {Id}", id);
        var casino = _casinos.FirstOrDefault(c => c.Id == id);
        
        if (casino == null)
        {
            return NotFound();
        }
        
        return Ok(casino);
    }

    [HttpPost]
    [RateLimit(PermitLimit = 10, Window = 60)] // Stricter rate limit for creation operations
    public IActionResult Create(CreateCasinoRequest request)
    {
        _logger.LogInformation("Creating new casino");
        
        var casino = new CasinoModel
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Rating = request.Rating,
            Location = request.Location
        };
        
        // In a real app, this would save to a database
        _casinos.Add(casino);
        
        return CreatedAtAction(nameof(GetById), new { id = casino.Id }, casino);
    }

    [HttpPut("{id}")]
    [RateLimit(PermitLimit = 10, Window = 60)] // Stricter rate limit for update operations
    public IActionResult Update(Guid id, UpdateCasinoRequest request)
    {
        _logger.LogInformation("Updating casino with ID {Id}", id);
        
        var casinoIndex = _casinos.FindIndex(c => c.Id == id);
        if (casinoIndex == -1)
        {
            return NotFound();
        }
        
        var casino = _casinos[casinoIndex];
        casino.Name = request.Name;
        casino.Rating = request.Rating;
        casino.Location = request.Location;
        
        _casinos[casinoIndex] = casino;
        
        return NoContent();
    }

    [HttpDelete("{id}")]
    [RateLimit(PermitLimit = 5, Window = 60)] // Very strict rate limit for deletion operations
    public IActionResult Delete(Guid id)
    {
        _logger.LogInformation("Deleting casino with ID {Id}", id);
        
        var casinoIndex = _casinos.FindIndex(c => c.Id == id);
        if (casinoIndex == -1)
        {
            return NotFound();
        }
        
        _casinos.RemoveAt(casinoIndex);
        
        return NoContent();
    }
}

public class CasinoModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public string Location { get; set; } = string.Empty;
}

public class CreateCasinoRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public string Location { get; set; } = string.Empty;
}

public class UpdateCasinoRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public string Location { get; set; } = string.Empty;
}