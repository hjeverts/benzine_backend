namespace Benzine.Api.DTOs;

public record VehicleRequest(string Naam, string? Merk, string? Type, int? Bouwjaar, DateOnly? Aankoopdatum);

public record VehicleResponse(
    int Id,
    string Naam,
    string? Merk,
    string? Type,
    int? Bouwjaar,
    DateOnly? Aankoopdatum,
    bool IsOwner,
    string EigenaarNaam,
    string? FotoDataUrl
);

public record ShareVehicleRequest(string Email);

public record VehicleShareResponse(Guid UserId, string Email, string Name, DateTime CreatedAt);

public record FuelEntryRequest(
    DateOnly Datum,
    int Odometer,
    string? BrandstofType,
    decimal Volume,
    decimal Bedrag,
    string? Tankstation,
    bool Vergeten
);

public record FuelEntryResponse(
    int Id,
    int VehicleId,
    DateOnly Datum,
    int Odometer,
    string? BrandstofType,
    decimal Volume,
    decimal Bedrag,
    string? Tankstation,
    bool Vergeten
);

public record MaintenanceEntryRequest(DateOnly Datum, int Odometer, int MaintenanceTypeId, string? Notitie);

public record MaintenanceEntryResponse(
    int Id,
    int VehicleId,
    DateOnly Datum,
    int Odometer,
    int MaintenanceTypeId,
    string MaintenanceTypeNaam,
    string? Notitie
);

public record MaintenanceTypeRequest(string Naam);
public record MaintenanceTypeResponse(int Id, string Naam);

// Statistieken t.b.v. dashboard/grafieken (vervangt Dash-plotly visualisaties)
public record VehicleStatsResponse(
    int VehicleId,
    decimal TotaleKosten,
    decimal TotaalLiters,
    decimal GemiddeldeVerbruikL100km,
    decimal GemiddeldePrijsPerLiter,
    int LaatsteOdometer
);
