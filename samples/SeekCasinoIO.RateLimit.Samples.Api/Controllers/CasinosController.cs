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
    private static readonly List<Casino> _casinos = new()
    {
        new Casino { Id = 1, Name = "Golden Star", Rating = 4.7m, Description = "A premium casino with a wide range of games." },
        new Casino { Id = 2, Name = "Royal Flush", Rating = 4.5m, Description = "Elegant and luxurious casino with top-notch service." },
        new Casino { Id = 3, Name = "Diamond Palace", Rating = 4.8m, Description = "The most exclusive casino in town with high-stakes tables." },
        new Casino { Id = 4, Name = "Silver Moon", Rating = 4.2m, Description = "A family-friendly casino with entertainment for all ages." },
        new Casino { Id = 5, Name = "Lucky Seven", Rating = 4.0m, Description = "A casual casino with a relaxed atmosphere." }
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
    public IActionResult GetById(int id)
    {
        _logger.LogInformation("Getting casino with ID {Id}", id);
        var casino = _casinos.FirstOrDefault(c => c.Id == id);
        
        if (casino == null)
        {
            return NotFound();
        }
        
        return Ok(casino);
    }

    [HttpPost]
    [RateLimit(PermitLimit = 10, Window = 60)] // More restrictive rate limit for POST requests
    public IActionResult Create(Casino casino)
    {
        _logger.LogInformation("Creating a new casino");
        
        casino.Id = _casinos.Max(c => c.Id) + 1;
        _casinos.Add(casino);
        
        return CreatedAtAction(nameof(GetById), new { id = casino.Id }, casino);
    }

    [HttpPut("{id}")]
    [RateLimit(PermitLimit = 10, Window = 60)] // More restrictive rate limit for PUT requests
    public IActionResult Update(int id, Casino casino)
    {
        _logger.LogInformation("Updating casino with ID {Id}", id);
        
        var existingCasino = _casinos.FirstOrDefault(c => c.Id == id);
        if (existingCasino == null)
        {
            return NotFound();
        }
        
        existingCasino.Name = casino.Name;
        existingCasino.Rating = casino.Rating;
        existingCasino.Description = casino.Description;
        
        return NoContent();
    }

    [HttpDelete("{id}")]
    [RateLimit(PermitLimit = 5, Window = 60)] // Very restrictive rate limit for DELETE requests
    public IActionResult Delete(int id)
    {
        _logger.LogInformation("Deleting casino with ID {Id}", id);
        
        var casino = _casinos.FirstOrDefault(c => c.Id == id);
        if (casino == null)
        {
            return NotFound();
        }
        
        _casinos.Remove(casino);
        
        return NoContent();
    }
}

public class Casino
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public string Description { get; set; } = string.Empty;
}