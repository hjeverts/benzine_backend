namespace Benzine.Api.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string Name { get; set; }
    public byte[]? Avatar { get; set; }
    public string? AvatarContentType { get; set; }
    public bool IsAdmin { get; set; }
    public byte[]? PasswordResetTokenHash { get; set; }
    public DateTime? PasswordResetExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Vehicle> Vehicles { get; set; } = [];
    public ICollection<VehicleShare> SharedVehicles { get; set; } = [];
}
