# Vehictory Backend

ASP.NET Core Web API (.NET 10) voor het bijhouden van brandstofkosten en onderhoud
per voertuig, per gebruiker. PostgreSQL als database, JWT-authenticatie voor
web (Angular) en mobiele (Kotlin/Android) clients.

## Projectstructuur

```
src/Vehictory.Api/
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

## Wachtwoord resetten

Wachtwoordresetlinks worden per e-mail verstuurd en zijn één uur geldig. Configureer
in `.env` een SMTP-server en de publieke URL waarop de Angular-app bereikbaar is:

```env
EMAIL_HOST=smtp.example.com
EMAIL_PORT=587
EMAIL_USE_SSL=true
EMAIL_USERNAME=gebruikersnaam
EMAIL_PASSWORD=app-wachtwoord
EMAIL_FROM=no-reply@example.com
EMAIL_FRONTEND_URL=https://vehictory.example.com
```

Voor lokale ontwikkeling is `EMAIL_FRONTEND_URL=http://localhost:4200`. Gebruik voor
een provider die dit vereist een app-wachtwoord, niet het normale accountwachtwoord.

## Gebruikersbeheer

Zet `ADMIN_EMAILS` in `.env` op het e-mailadres van de eerste beheerder (meerdere
adressen scheid je met komma's). Bij opstarten krijgen bestaande accounts met die
e-mailadressen de beheerrol. Een beheerder kan daarna via de gebruikersbeheerpagina
andere accounts bewerken, promoveren of verwijderen.

## Lokaal draaien zonder Docker (development)

```bash
cd src/Vehictory.Api
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
