using Benzine.Api.Data;
using Benzine.Api.DTOs;
using Benzine.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Benzine.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/vehicles/{vehicleId:int}/maintenance")]
public class MaintenanceEntriesController(BenzineDbContext db) : ControllerBase
{
    private async Task<Vehicle?> GetOwnedVehicle(int vehicleId)
    {
        var userId = this.GetUserId();
        return await db.Vehicles.SingleOrDefaultAsync(v => v.Id == vehicleId && v.UserId == userId);
    }

    [HttpGet]
    public async Task<ActionResult<List<MaintenanceEntryResponse>>> GetAll(int vehicleId)
    {
        if (await GetOwnedVehicle(vehicleId) is null) return NotFound();

        var entries = await db.MaintenanceEntries
            .Include(m => m.MaintenanceType)
            .Where(m => m.VehicleId == vehicleId)
            .OrderByDescending(m => m.Datum)
            .Select(m => new MaintenanceEntryResponse(
                m.Id, m.VehicleId, m.Datum, m.Odometer, m.MaintenanceTypeId, m.MaintenanceType!.Naam, m.Notitie))
            .ToListAsync();

        return Ok(entries);
    }

    [HttpPost]
    public async Task<ActionResult<MaintenanceEntryResponse>> Create(int vehicleId, MaintenanceEntryRequest request)
    {
        if (await GetOwnedVehicle(vehicleId) is null) return NotFound();

        var type = await db.MaintenanceTypes.FindAsync(request.MaintenanceTypeId);
        if (type is null) return BadRequest("Onbekend onderhoudstype.");

        var entry = new MaintenanceEntry
        {
            VehicleId = vehicleId,
            Datum = request.Datum,
            Odometer = request.Odometer,
            MaintenanceTypeId = request.MaintenanceTypeId,
            Notitie = request.Notitie,
        };

        db.MaintenanceEntries.Add(entry);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { vehicleId },
            new MaintenanceEntryResponse(entry.Id, entry.VehicleId, entry.Datum, entry.Odometer, entry.MaintenanceTypeId, type.Naam, entry.Notitie));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int vehicleId, int id)
    {
        if (await GetOwnedVehicle(vehicleId) is null) return NotFound();

        var entry = await db.MaintenanceEntries.SingleOrDefaultAsync(m => m.Id == id && m.VehicleId == vehicleId);
        if (entry is null) return NotFound();

        db.MaintenanceEntries.Remove(entry);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
