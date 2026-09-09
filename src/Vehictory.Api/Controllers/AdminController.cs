using Vehictory.Api.Data;
using Vehictory.Api.DTOs;
using Vehictory.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Vehictory.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/users")]
public class AdminController(VehictoryDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminUserResponse>>> GetUsers(CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdmin(cancellationToken)) return Forbid();

        var users = await db.Users
            .OrderBy(u => u.Name)
            .Select(u => new AdminUserResponse(u.Id, u.Email, u.Name, u.IsAdmin, u.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminUserResponse>> UpdateUser(
        Guid id,
        UpdateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsCurrentUserAdmin(cancellationToken)) return Forbid();

        var email = request.Email.Trim().ToLowerInvariant();
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(name))
            return BadRequest("Naam en e-mailadres zijn verplicht.");

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return NotFound();
        if (await db.Users.AnyAsync(u => u.Email == email && u.Id != id, cancellationToken))
            return Conflict("Er bestaat al een account met dit e-mailadres.");
        if (user.IsAdmin && !request.IsAdmin && await db.Users.CountAsync(u => u.IsAdmin, cancellationToken) == 1)
            return BadRequest("De laatste beheerder kan niet worden gedegradeerd.");

        user.Email = email;
        user.Name = name;
        user.IsAdmin = request.IsAdmin;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(user));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = this.GetUserId();
        if (!await db.Users.AnyAsync(u => u.Id == currentUserId && u.IsAdmin, cancellationToken))
            return Forbid();
        if (id == currentUserId)
            return BadRequest("Je kunt je eigen account niet via gebruikersbeheer verwijderen.");

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return NotFound();
        if (user.IsAdmin && await db.Users.CountAsync(u => u.IsAdmin, cancellationToken) == 1)
            return BadRequest("De laatste beheerder kan niet worden verwijderd.");

        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Task<bool> IsCurrentUserAdmin(CancellationToken cancellationToken) =>
        db.Users.AnyAsync(u => u.Id == this.GetUserId() && u.IsAdmin, cancellationToken);

    private static AdminUserResponse ToResponse(User user) =>
        new(user.Id, user.Email, user.Name, user.IsAdmin, user.CreatedAt);
}
