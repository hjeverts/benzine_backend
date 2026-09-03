using Benzine.Api.Data;
using Benzine.Api.DTOs;
using Benzine.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Benzine.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/maintenance-types")]
public class MaintenanceTypesController(BenzineDbContext db) : ControllerBase
{
    // Onderhoudstypes zijn gedeeld tussen alle gebruikers (zoals in de oude Dash-app).

    [HttpGet]
    public async Task<ActionResult<List<MaintenanceTypeResponse>>> GetAll()
    {
        var types = await db.MaintenanceTypes
            .Select(t => new MaintenanceTypeResponse(t.Id, t.Naam))
            .ToListAsync();
        return Ok(types);
    }

    [HttpPost]
    public async Task<ActionResult<MaintenanceTypeResponse>> Create(MaintenanceTypeRequest request)
    {
        if (await db.MaintenanceTypes.AnyAsync(t => t.Naam == request.Naam))
            return Conflict("Dit onderhoudstype bestaat al.");

        var type = new MaintenanceType { Naam = request.Naam };
        db.MaintenanceTypes.Add(type);
        await db.SaveChangesAsync();

        return Ok(new MaintenanceTypeResponse(type.Id, type.Naam));
    }
}
