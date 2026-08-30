# Hà Minh Anh — điều xe & đối soát cước

Desktop app (.NET 10 + WPF) for Công ty TNHH DV vận tải & TM Hà Minh Anh. Phase 1 follows `docs/brief.md`: customers → dispatch orders → partner vehicles/drivers → documents → reconcile → monthly freight statement.

## Layout
- `legacy/` — original VB.NET + Plexis (reference only)
- `docs/` — brief, schema mapping, module map, ETL cutover
- `database/` — SQL schema + ETL (Phase 1 skips cash/VAT scripts)
- `src/` — `Hma.slnx`

## Architecture
`Desktop WPF` → `Application` → `Domain`  
`Infrastructure.SqlServer` and `Reporting` are adapters. A future web UI can reuse Application + Domain.

## Run
1. SQL Server LocalDB (`MSSQLLocalDB`)
2. `dotnet run --project src/Hma.Desktop.Wpf`
3. Login `admin` / `admin123` (manager) or `ketoan` / `ketoan123` (accountant)

First start creates database `Hma`. If an older clone-schema database exists, it is dropped and recreated. An empty database (no customers yet) also loads realistic demo trips so Dashboard / Đối soát / Bảng kê have data; ETL or an existing customer list is left untouched.

## Cutover from DHXE
Legacy MDF is SQL Server 2000 and cannot attach to modern LocalDB. See `docs/etl-cutover.md`.
