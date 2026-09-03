namespace Benzine.Api.Models;

/// <summary>
/// Koppelt een voertuig aan een extra account (bv. een partner) met leestoegang
/// en het recht om tankbeurten/onderhoud toe te voegen. Alleen de eigenaar
/// (Vehicle.UserId) mag het voertuig zelf bewerken/verwijderen of delen beheren.
/// </summary>
public class VehicleShare
{
    public int Id { get; set; }
    public required int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public required Guid UserId { get; set; }
    public User? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
