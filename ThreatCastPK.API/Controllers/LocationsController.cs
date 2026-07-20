using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThreatCastPK.Database.Context;

namespace ThreatCastPK.API.Controllers;

[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly ThreatCastDbContext _context;

    public LocationsController(ThreatCastDbContext context)
    {
        _context = context;
    }

    [HttpGet("cities")]
    public async Task<IActionResult> GetCities()
    {
        var cities = await _context.Locations
            .OrderBy(l => l.CityName)
            .Select(l => l.CityName)
            .ToListAsync();

        return Ok(cities);
    }
}