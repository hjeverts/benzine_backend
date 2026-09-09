# Data-migratie: Benzine-2.0 (Dash/parquet) → vehictory_backend (PostgreSQL)

Eenmalig script om je bestaande brandstof- en onderhoudsdata over te zetten
naar de nieuwe stack. De oude app kende geen accounts per voertuig (alle
voertuigen waren globaal zichtbaar voor de 2 hardcoded gebruikers uit
`config.yaml`); de nieuwe app werkt met self-service registratie en
voertuigen die aan één account hangen. Kies daarom vooraf welk account
(e-mailadres) eigenaar wordt van alle gemigreerde voertuigen.

## Stappen

1. **Registreer eerst een account** via de Angular-frontend (`/register`),
   bijvoorbeeld met je eigen e-mailadres. De oude bcrypt-wachtwoorden uit
   `config.yaml` worden **niet** automatisch overgenomen (nieuw account = nieuw
   wachtwoord) — dat is bewust, om het oude cookie-secret/hash-bestand niet
   opnieuw te hoeven vertrouwen.
2. Zorg dat je de PostgreSQL-database van `vehictory_backend` kunt bereiken
   (lokaal via `docker compose port postgres 5432`, of direct op de server).
3. Installeer dependencies met [uv](https://docs.astral.sh/uv/):
   ```bash
   cd vehictory_backend/scripts
   uv sync
   ```
4. Draai eerst een dry-run om te zien wat er gemigreerd zou worden:
   ```bash
   uv run migrate_data.py \
     --data-dir /home/docker/benzine/data \
     --database-url "postgresql://benzine:WACHTWOORD@localhost:5432/benzine" \
     --owner-email jij@example.com \
     --dry-run
   ```
5. Als de aantallen kloppen, draai het script echt (zonder `--dry-run`):
   ```bash
   uv run migrate_data.py \
     --data-dir /home/docker/benzine/data \
     --database-url "postgresql://benzine:WACHTWOORD@localhost:5432/benzine" \
     --owner-email jij@example.com
   ```

`--data-dir` moet wijzen naar de map met `voertuigen.parquet`,
`benzine-*.parquet`, `onderhoud.parquet` en `onderhoud_types.parquet` (op de
Ubuntu-server is dit vermoedelijk `/home/docker/benzine/data`).

## Idempotentie

- Voertuigen worden gematcht op naam (binnen het opgegeven account) — al
  bestaande voertuigen worden niet dubbel aangemaakt.
- Onderhoudstypes worden gematcht op naam (globaal) — ook niet dubbel.
- Tankbeurten en onderhoudsregels worden **altijd** opnieuw ingevoegd. Draai
  dit script dus in principe maar één keer per dataset; bij een tweede run op
  dezelfde data krijg je dubbele tankbeurten/onderhoudsregels.

## Tweede account (bv. je partner)

Wil je voertuigen splitsen over meerdere accounts (bv. iemand anders had ook
toegang tot de oude Dash-app)? Registreer een tweede account via de frontend
en draai het script opnieuw met een `--data-dir` die alleen de relevante
`benzine-*.parquet`-bestanden voor die voertuigen bevat, of pas het script aan
om een expliciete naam→account-mapping te ondersteunen.
