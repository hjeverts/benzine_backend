using Benzine.Api.Data;
using Benzine.Api.DTOs;
using Benzine.Api.Models;
using Benzine.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Benzine.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(BenzineDbContext db, JwtTokenService jwtService) : ControllerBase
{
    private const long MaxImageSize = 2 * 1024 * 1024;

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
        return Ok(ToAuthResponse(user, token));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == request.Email.Trim().ToLowerInvariant());
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Ongeldige inloggegevens.");

        var token = jwtService.GenerateToken(user);
        return Ok(ToAuthResponse(user, token));
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<ActionResult<ProfileResponse>> GetProfile()
    {
        var user = await GetCurrentUser();
        return Ok(new ProfileResponse(user.Email, user.Name, ToDataUrl(user.AvatarContentType, user.Avatar)));
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<AuthResponse>> UpdateProfile(UpdateProfileRequest request)
    {
        var user = await GetCurrentUser();
        var email = request.Email.Trim().ToLowerInvariant();
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(name))
            return BadRequest("Naam en e-mailadres zijn verplicht.");
        if (await db.Users.AnyAsync(u => u.Email == email && u.Id != user.Id))
            return Conflict("Er bestaat al een account met dit e-mailadres.");

        user.Email = email;
        user.Name = name;
        await db.SaveChangesAsync();
        return Ok(ToAuthResponse(user, jwtService.GenerateToken(user)));
    }

    [Authorize]
    [HttpPut("profile/password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var user = await GetCurrentUser();
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest("Het huidige wachtwoord is onjuist.");
        if (request.NewPassword.Length < 8)
            return BadRequest("Het nieuwe wachtwoord moet minimaal 8 tekens bevatten.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpPut("profile/avatar")]
    public async Task<ActionResult<ProfileResponse>> UpdateAvatar(IFormFile file)
    {
        var image = await ReadImage(file);
        if (image.Error is not null) return BadRequest(image.Error);

        var user = await GetCurrentUser();
        user.Avatar = image.Content;
        user.AvatarContentType = image.ContentType;
        await db.SaveChangesAsync();
        return Ok(new ProfileResponse(user.Email, user.Name, ToDataUrl(user.AvatarContentType, user.Avatar)));
    }

    private async Task<User> GetCurrentUser() =>
        await db.Users.SingleAsync(u => u.Id == this.GetUserId());

    private static AuthResponse ToAuthResponse(User user, string token) =>
        new(token, user.Email, user.Name, ToDataUrl(user.AvatarContentType, user.Avatar));

    internal static async Task<(byte[]? Content, string? ContentType, string? Error)> ReadImage(IFormFile? file)
    {
        if (file is null || file.Length == 0) return (null, null, "Selecteer een afbeelding.");
        if (file.Length > MaxImageSize) return (null, null, "De afbeelding mag maximaal 2 MB groot zijn.");

        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        var content = memory.ToArray();
        var contentType = content switch
        {
            [0xFF, 0xD8, ..] => "image/jpeg",
            [0x89, 0x50, 0x4E, 0x47, ..] => "image/png",
            [0x52, 0x49, 0x46, 0x46, ..] when content.Length >= 12
                && content.AsSpan(8, 4).SequenceEqual("WEBP"u8) => "image/webp",
            _ => null,
        };
        return contentType is null
            ? (null, null, "Gebruik een JPEG-, PNG- of WebP-afbeelding.")
            : (content, contentType, null);
    }

    internal static string? ToDataUrl(string? contentType, byte[]? content) =>
        content is null || contentType is null ? null : $"data:{contentType};base64,{Convert.ToBase64String(content)}";
}
