namespace Vehictory.Api.Models;

public class MaintenanceType
{
    public int Id { get; set; }
    public required string Naam { get; set; }
    // Onderhoudstypes zijn globaal (niet per gebruiker), zoals in de oude Dash-app.

    public ICollection<MaintenanceEntry> MaintenanceEntries { get; set; } = [];
}
