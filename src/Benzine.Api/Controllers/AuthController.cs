using Benzine.Api.Data;
using Benzine.Api.DTOs;
using Benzine.Api.Models;
using Benzine.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Benzine.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(BenzineDbContext db, JwtTokenService jwtService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (await db.Users.AnyAsync(u => u.Email == request.Email))
            return Conflict("Er bestaat al een account met dit e-mailadres.");

        var user = new User
        {
            Email = request.Email.Trim().ToLowerInvariant(),
            Name = request.Name,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = jwtService.GenerateToken(user);
        return Ok(new AuthResponse(token, user.Email, user.Name));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == request.Email.Trim().ToLowerInvariant());
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Ongeldige inloggegevens.");

        var token = jwtService.GenerateToken(user);
        return Ok(new AuthResponse(token, user.Email, user.Name));
    }
}
