using Benzine.Api.Data;
using Benzine.Api.DTOs;
using Benzine.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Benzine.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/vehicles/{vehicleId:int}/fuel")]
public class FuelEntriesController(BenzineDbContext db) : ControllerBase
{
    // Eigenaar én iedereen met wie het voertuig gedeeld is, mogen tankbeurten lezen/toevoegen/verwijderen.
    private async Task<Vehicle?> GetAccessibleVehicle(int vehicleId)
    {
        var userId = this.GetUserId();
        return await db.Vehicles.SingleOrDefaultAsync(v =>
            v.Id == vehicleId && (v.UserId == userId || v.Shares.Any(s => s.UserId == userId)));
    }

    private static FuelEntryResponse ToResponse(FuelEntry f) => new(
        f.Id, f.VehicleId, f.Datum, f.Odometer, f.BrandstofType, f.Volume, f.Bedrag, f.Tankstation, f.Vergeten);

    [HttpGet]
    public async Task<ActionResult<List<FuelEntryResponse>>> GetAll(int vehicleId)
    {
        if (await GetAccessibleVehicle(vehicleId) is null) return NotFound();

        var entries = await db.FuelEntries
            .Where(f => f.VehicleId == vehicleId)
            .OrderByDescending(f => f.Datum)
            .Select(f => ToResponse(f))
            .ToListAsync();

        return Ok(entries);
    }

    [HttpPost]
    public async Task<ActionResult<FuelEntryResponse>> Create(int vehicleId, FuelEntryRequest request)
    {
        if (await GetAccessibleVehicle(vehicleId) is null) return NotFound();

        var entry = new FuelEntry
        {
            VehicleId = vehicleId,
            Datum = request.Datum,
            Odometer = request.Odometer,
            BrandstofType = request.BrandstofType,
            Volume = request.Volume,
            Bedrag = request.Bedrag,
            Tankstation = request.Tankstation,
            Vergeten = request.Vergeten,
        };

        db.FuelEntries.Add(entry);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { vehicleId }, ToResponse(entry));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int vehicleId, int id)
    {
        if (await GetAccessibleVehicle(vehicleId) is null) return NotFound();

        var entry = await db.FuelEntries.SingleOrDefaultAsync(f => f.Id == id && f.VehicleId == vehicleId);
        if (entry is null) return NotFound();

        db.FuelEntries.Remove(entry);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // Vervangt de Dash plotly-visualisaties: geaggregeerde statistieken voor het dashboard.
    [HttpGet("/api/vehicles/{vehicleId:int}/stats")]
    public async Task<ActionResult<VehicleStatsResponse>> GetStats(int vehicleId)
    {
        if (await GetAccessibleVehicle(vehicleId) is null) return NotFound();

        var entries = await db.FuelEntries
            .Where(f => f.VehicleId == vehicleId)
            .OrderBy(f => f.Odometer)
            .ToListAsync();

        if (entries.Count == 0)
            return Ok(new VehicleStatsResponse(vehicleId, 0, 0, 0, 0, 0));

        var totaleKosten = entries.Sum(f => f.Bedrag);
        var totaalLiters = entries.Sum(f => f.Volume);
        var eersteOdometer = entries.First().Odometer;
        var laatsteOdometer = entries.Last().Odometer;
        var geldigeTankbeurten = entries
            .Skip(1)
            .Select((entry, index) => new { Entry = entry, Afstand = entry.Odometer - entries[index].Odometer })
            .Where(x => !x.Entry.Vergeten && x.Afstand > 0)
            .ToList();
        var afstand = geldigeTankbeurten.Sum(x => x.Afstand);
        var litersVoorVerbruik = geldigeTankbeurten.Sum(x => x.Entry.Volume);

        var verbruikL100km = afstand > 0 ? (litersVoorVerbruik / afstand) * 100m : 0;
        var gemPrijsPerLiter = totaalLiters > 0 ? totaleKosten / totaalLiters : 0;

        return Ok(new VehicleStatsResponse(
            vehicleId, totaleKosten, totaalLiters, verbruikL100km, gemPrijsPerLiter, laatsteOdometer));
    }
}
