using Benzine.Api.Data;
using Benzine.Api.DTOs;
using Benzine.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Benzine.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/vehicles")]
public class VehiclesController(BenzineDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<VehicleResponse>>> GetAll()
    {
        var userId = this.GetUserId();
        var vehicles = await db.Vehicles
            .Where(v => v.UserId == userId)
            .Select(v => new VehicleResponse(v.Id, v.Naam, v.Merk, v.Type, v.Bouwjaar, v.Aankoopdatum))
            .ToListAsync();
        return Ok(vehicles);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VehicleResponse>> GetById(int id)
    {
        var userId = this.GetUserId();
        var vehicle = await db.Vehicles.SingleOrDefaultAsync(v => v.Id == id && v.UserId == userId);
        if (vehicle is null) return NotFound();

        return Ok(new VehicleResponse(vehicle.Id, vehicle.Naam, vehicle.Merk, vehicle.Type, vehicle.Bouwjaar, vehicle.Aankoopdatum));
    }

    [HttpPost]
    public async Task<ActionResult<VehicleResponse>> Create(VehicleRequest request)
    {
        var userId = this.GetUserId();
        var vehicle = new Vehicle
        {
            UserId = userId,
            Naam = request.Naam,
            Merk = request.Merk,
            Type = request.Type,
            Bouwjaar = request.Bouwjaar,
            Aankoopdatum = request.Aankoopdatum,
        };

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        var response = new VehicleResponse(vehicle.Id, vehicle.Naam, vehicle.Merk, vehicle.Type, vehicle.Bouwjaar, vehicle.Aankoopdatum);
        return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, VehicleRequest request)
    {
        var userId = this.GetUserId();
        var vehicle = await db.Vehicles.SingleOrDefaultAsync(v => v.Id == id && v.UserId == userId);
        if (vehicle is null) return NotFound();

        vehicle.Naam = request.Naam;
        vehicle.Merk = request.Merk;
        vehicle.Type = request.Type;
        vehicle.Bouwjaar = request.Bouwjaar;
        vehicle.Aankoopdatum = request.Aankoopdatum;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = this.GetUserId();
        var vehicle = await db.Vehicles.SingleOrDefaultAsync(v => v.Id == id && v.UserId == userId);
        if (vehicle is null) return NotFound();

        db.Vehicles.Remove(vehicle);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
