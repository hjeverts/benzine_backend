namespace Vehictory.Api.Models;

public class FuelEntry
{
    public int Id { get; set; }
    public required int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public required DateOnly Datum { get; set; }
    public required int Odometer { get; set; }
    public string? BrandstofType { get; set; }
    public required decimal Volume { get; set; }      // liters
    public required decimal Bedrag { get; set; }       // euro's
    public string? Tankstation { get; set; }
    public bool Vergeten { get; set; }                 // gemiste registratie (bekend uit oude app)
}
