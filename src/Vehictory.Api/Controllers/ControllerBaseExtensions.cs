using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Vehictory.Api.Controllers;

public static class ControllerBaseExtensions
{
    public static Guid GetUserId(this Microsoft.AspNetCore.Mvc.ControllerBase controller)
    {
        var sub = controller.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? controller.User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new UnauthorizedAccessException("Geen geldige user-claim gevonden.");
        return Guid.Parse(sub);
    }
}
