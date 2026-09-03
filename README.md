# Benzine Backend

ASP.NET Core Web API (.NET 10) voor het bijhouden van brandstofkosten en onderhoud
per voertuig, per gebruiker. PostgreSQL als database, JWT-authenticatie voor
web (Angular) en mobiele (Kotlin/Android) clients.

## Projectstructuur

```
src/Benzine.Api/
  Controllers/   API-endpoints (Auth, Vehicles, FuelEntries, MaintenanceEntries, MaintenanceTypes)
  Models/        EF Core entities
  DTOs/          Request/response-modellen
  Services/      JwtTokenService
  Data/          DbContext + migraties
```

## Lokaal draaien met Docker

1. Kopieer `.env.example` naar `.env` en vul een sterke `POSTGRES_PASSWORD` en `JWT_KEY` in
   (`openssl rand -base64 48` voor de JWT-key).
2. `docker compose up --build`
3. API draait op `http://localhost:5080`, Swagger/OpenAPI op `/openapi/v1.json` (Development only).

## Lokaal draaien zonder Docker (development)

```bash
cd src/Benzine.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=benzine;Username=benzine;Password=..."
dotnet user-secrets set "Jwt:Key" "een-lange-random-string-van-minimaal-32-tekens"
dotnet ef database update
dotnet run
```

## Belangrijkste endpoints

| Methode | Route                                          | Omschrijving                      |
|---------|-------------------------------------------------|------------------------------------|
| POST    | /api/auth/register                              | Account aanmaken                   |
| POST    | /api/auth/login                                 | Inloggen, geeft JWT terug          |
| GET     | /api/vehicles                                   | Eigen voertuigen                   |
| POST    | /api/vehicles                                   | Voertuig toevoegen                 |
| GET     | /api/vehicles/{id}/fuel                         | Tankbeurten van voertuig           |
| POST    | /api/vehicles/{id}/fuel                         | Tankbeurt registreren              |
| GET     | /api/vehicles/{id}/stats                        | Verbruik/kosten-statistieken       |
| GET     | /api/vehicles/{id}/maintenance                  | Onderhoudshistorie                 |
| GET     | /api/maintenance-types                          | Beschikbare onderhoudstypes        |

Alle `vehicles`-routes zijn gescoped op de ingelogde gebruiker (JWT `sub`-claim) —
gebruikers zien alleen hun eigen voertuigen en data.

## Data-migratie vanuit oude Dash-app (Benzine 2.0)

De oude app gebruikte parquet-bestanden (`voertuigen.parquet`, `data/benzine-*.parquet`).
Een eenmalig migratiescript (buiten deze repo) leest deze in met pandas/pyarrow en
schrijft ze via de `/api/auth/register` + `/api/vehicles` + `/api/vehicles/{id}/fuel`
endpoints (of rechtstreeks via EF Core seed) naar deze nieuwe database.
