#!/usr/bin/env python3
"""
Migreert de oude Dash/parquet-data (Benzine-2.0) naar de nieuwe PostgreSQL-database
van benzine_backend.

Belangrijk: de nieuwe app werkt met accounts + eigen voertuigen per account. Dit
script hangt ALLE gemigreerde voertuigen (en hun tankbeurten/onderhoud) aan één
al bestaand account, aangegeven met --owner-email. Maak dat account dus EERST aan
via de registratiepagina van de Angular-frontend, en geef hier hetzelfde
e-mailadres op.

Gebruik (met uv, vanuit deze map):
    uv sync
    uv run migrate_data.py \
        --data-dir /home/docker/benzine/data \
        --database-url "postgresql://benzine:WACHTWOORD@localhost:5432/benzine" \
        --owner-email jij@example.com

Voeg --dry-run toe om alleen te tellen wat er gemigreerd zou worden, zonder te
schrijven. Het script is veilig om opnieuw te draaien: voertuigen (op naam) en
onderhoudstypes (op naam) worden niet dubbel aangemaakt. Tankbeurten en
onderhoudsregels worden wel altijd opnieuw ingevoegd, dus draai dit per
voertuig/dataset in principe maar één keer.
"""

from __future__ import annotations

import argparse
import os
import sys
from datetime import date
from pathlib import Path

import pandas as pd
import psycopg


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument(
        "--data-dir",
        required=True,
        type=Path,
        help="Pad naar de oude 'data'-map van Benzine-2.0 (bevat voertuigen.parquet, benzine-*.parquet, onderhoud*.parquet)",
    )
    parser.add_argument(
        "--database-url",
        default=os.environ.get("DATABASE_URL"),
        help="PostgreSQL-connectiestring, bv. postgresql://user:wachtwoord@host:5432/benzine (of zet env var DATABASE_URL)",
    )
    parser.add_argument(
        "--owner-email",
        required=True,
        help="E-mailadres van het account (al aangemaakt via de frontend) waaraan alle voertuigen gekoppeld worden",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Alleen tellen/loggen, niets wegschrijven naar de database",
    )
    args = parser.parse_args()
    if not args.database_url:
        parser.error("geef --database-url op of zet de env var DATABASE_URL")
    return args


def laad_voertuigen(data_dir: Path) -> pd.DataFrame:
    pad = data_dir / "voertuigen.parquet"
    if not pad.exists():
        sys.exit(f"Kan {pad} niet vinden.")
    return pd.read_parquet(pad)


def laad_onderhoud_types(data_dir: Path) -> pd.DataFrame:
    pad = data_dir / "onderhoud_types.parquet"
    if not pad.exists():
        return pd.DataFrame(columns=["type_naam"])
    return pd.read_parquet(pad)


def laad_onderhoud(data_dir: Path) -> pd.DataFrame:
    pad = data_dir / "onderhoud.parquet"
    if not pad.exists():
        return pd.DataFrame(columns=["datum", "voertuig_naam", "odometer", "onderhoud_type", "notitie"])
    return pd.read_parquet(pad)


def laad_brandstof_bestanden(data_dir: Path) -> list[Path]:
    return sorted(data_dir.glob("benzine-*.parquet"))


def main() -> None:
    args = parse_args()
    data_dir: Path = args.data_dir
    if not data_dir.is_dir():
        sys.exit(f"--data-dir {data_dir} bestaat niet of is geen map.")

    voertuigen = laad_voertuigen(data_dir)
    onderhoud_types = laad_onderhoud_types(data_dir)
    onderhoud = laad_onderhoud(data_dir)
    brandstof_bestanden = laad_brandstof_bestanden(data_dir)

    print(f"Gevonden: {len(voertuigen)} voertuigen, {len(brandstof_bestanden)} brandstofbestanden, "
          f"{len(onderhoud)} onderhoudsregels, {len(onderhoud_types)} onderhoudstypes.")

    with psycopg.connect(args.database_url) as conn:
        with conn.cursor() as cur:
            cur.execute('SELECT "Id" FROM "Users" WHERE "Email" = %s', (args.owner_email.strip().lower(),))
            row = cur.fetchone()
            if row is None:
                sys.exit(
                    f"Geen account gevonden met e-mailadres '{args.owner_email}'. "
                    "Maak dit account eerst aan via de registratiepagina van de frontend en probeer opnieuw."
                )
            owner_id = row[0]

            # --- Voertuigen ---
            naam_naar_vehicle_id: dict[str, int] = {}
            merktype_naar_naam: dict[tuple[str, str], str] = {}
            nieuw_voertuigen = 0
            for _, r in voertuigen.iterrows():
                naam = str(r["Naam"]).strip()
                merk = str(r["Merk"]).strip()
                vtype = str(r["Type"]).strip()
                merktype_naar_naam[(merk, vtype)] = naam

                cur.execute(
                    'SELECT "Id" FROM "Vehicles" WHERE "UserId" = %s AND "Naam" = %s',
                    (owner_id, naam),
                )
                bestaand = cur.fetchone()
                if bestaand is not None:
                    naam_naar_vehicle_id[naam] = bestaand[0]
                    continue

                bouwjaar = int(r["Jaar"]) if pd.notna(r["Jaar"]) else None
                aankoopjaar = int(r["Aankoop"]) if pd.notna(r["Aankoop"]) else None
                # Oude data kent alleen een aankoopJAAR (geen exacte datum); we slaan dit
                # op als 1 januari van dat jaar zodat het als DateOnly past.
                aankoopdatum = date(aankoopjaar, 1, 1) if aankoopjaar else None

                if args.dry_run:
                    naam_naar_vehicle_id[naam] = -1
                    nieuw_voertuigen += 1
                    continue

                cur.execute(
                    """
                    INSERT INTO "Vehicles" ("UserId", "Naam", "Merk", "Type", "Bouwjaar", "Aankoopdatum")
                    VALUES (%s, %s, %s, %s, %s, %s)
                    RETURNING "Id"
                    """,
                    (owner_id, naam, merk, vtype, bouwjaar, aankoopdatum),
                )
                naam_naar_vehicle_id[naam] = cur.fetchone()[0]
                nieuw_voertuigen += 1
            print(f"Voertuigen: {nieuw_voertuigen} nieuw aangemaakt, {len(naam_naar_vehicle_id) - nieuw_voertuigen} bestonden al.")

            # --- Onderhoudstypes (globaal, niet per gebruiker) ---
            type_naar_id: dict[str, int] = {}
            alle_types = set(onderhoud_types["type_naam"].dropna().astype(str).str.strip()) | \
                set(onderhoud["onderhoud_type"].dropna().astype(str).str.strip()) if not onderhoud.empty else \
                set(onderhoud_types["type_naam"].dropna().astype(str).str.strip())
            nieuw_types = 0
            for naam in sorted(alle_types):
                cur.execute('SELECT "Id" FROM "MaintenanceTypes" WHERE "Naam" = %s', (naam,))
                bestaand = cur.fetchone()
                if bestaand is not None:
                    type_naar_id[naam] = bestaand[0]
                    continue
                if args.dry_run:
                    type_naar_id[naam] = -1
                    nieuw_types += 1
                    continue
                cur.execute(
                    'INSERT INTO "MaintenanceTypes" ("Naam") VALUES (%s) RETURNING "Id"',
                    (naam,),
                )
                type_naar_id[naam] = cur.fetchone()[0]
                nieuw_types += 1
            print(f"Onderhoudstypes: {nieuw_types} nieuw aangemaakt, {len(type_naar_id) - nieuw_types} bestonden al.")

            # --- Tankbeurten ---
            nieuwe_fuel_entries = 0
            overgeslagen_fuel = 0
            for bestand in brandstof_bestanden:
                df = pd.read_parquet(bestand)
                for _, r in df.iterrows():
                    merk = str(r.get("voertuig_merk", "")).strip()
                    vtype = str(r.get("voertuig_type", "")).strip()
                    naam = merktype_naar_naam.get((merk, vtype))
                    if naam is None or naam not in naam_naar_vehicle_id:
                        overgeslagen_fuel += 1
                        continue
                    vehicle_id = naam_naar_vehicle_id[naam]

                    datum = pd.to_datetime(r["datum"]).date()
                    odometer = int(r["odometer"]) if pd.notna(r["odometer"]) else 0
                    volume = float(r["volume"]) if pd.notna(r["volume"]) else 0.0
                    bedrag = float(r["bedrag"]) if pd.notna(r["bedrag"]) else 0.0
                    brandstof = r.get("brandstof")
                    tankstation = r.get("tankstation")
                    vergeten = bool(r.get("vergeten", False))

                    if args.dry_run:
                        nieuwe_fuel_entries += 1
                        continue

                    cur.execute(
                        """
                        INSERT INTO "FuelEntries"
                            ("VehicleId", "Datum", "Odometer", "BrandstofType", "Volume", "Bedrag", "Tankstation", "Vergeten")
                        VALUES (%s, %s, %s, %s, %s, %s, %s, %s)
                        """,
                        (vehicle_id, datum, odometer, brandstof, volume, bedrag, tankstation, vergeten),
                    )
                    nieuwe_fuel_entries += 1
            print(f"Tankbeurten: {nieuwe_fuel_entries} ingevoegd, {overgeslagen_fuel} overgeslagen (voertuig niet gevonden).")

            # --- Onderhoud ---
            nieuwe_maintenance_entries = 0
            overgeslagen_onderhoud = 0
            for _, r in onderhoud.iterrows():
                naam = str(r["voertuig_naam"]).strip()
                vehicle_id = naam_naar_vehicle_id.get(naam)
                if vehicle_id is None:
                    overgeslagen_onderhoud += 1
                    continue

                type_naam = str(r["onderhoud_type"]).strip()
                type_id = type_naar_id.get(type_naam)
                if type_id is None:
                    overgeslagen_onderhoud += 1
                    continue

                datum = pd.to_datetime(r["datum"]).date()
                odometer = int(r["odometer"]) if pd.notna(r["odometer"]) else 0
                notitie = r.get("notitie") or None

                if args.dry_run:
                    nieuwe_maintenance_entries += 1
                    continue

                cur.execute(
                    """
                    INSERT INTO "MaintenanceEntries" ("VehicleId", "Datum", "Odometer", "MaintenanceTypeId", "Notitie")
                    VALUES (%s, %s, %s, %s, %s)
                    """,
                    (vehicle_id, datum, odometer, type_id, notitie),
                )
                nieuwe_maintenance_entries += 1
            print(f"Onderhoud: {nieuwe_maintenance_entries} ingevoegd, {overgeslagen_onderhoud} overgeslagen.")

        if args.dry_run:
            conn.rollback()
            print("\n--dry-run: niets weggeschreven (transactie teruggedraaid).")
        else:
            conn.commit()
            print("\nMigratie voltooid en opgeslagen.")


if __name__ == "__main__":
    main()
