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
1. Cài SQL Server LocalDB (`MSSQLLocalDB`) hoặc SQL Server 2016+, rồi đặt `HMA_CONNECTION`.
2. Với database trống, đặt `HMA_BOOTSTRAP_ADMIN_PASSWORD` bằng mật khẩu mạnh (ít nhất 12 ký tự, có hoa/thường/số/ký tự đặc biệt).
3. Chạy `dotnet run --project src/Hma.Desktop.Wpf`; đăng nhập `admin` bằng mật khẩu bootstrap và xóa biến bootstrap sau lần tạo đầu tiên.

First start migrates and seeds database `Hma`. An empty database also loads realistic demo catalog data; ETL or an existing customer list is left untouched. The application never ships a default password.

## Cutover from DHXE
Legacy MDF is SQL Server 2000 and cannot attach to modern LocalDB. See `docs/etl-cutover.md`.

See `docs/Architecture.md` and `docs/Database.md` for the implemented design. Deployment, health checks, backup, restore, and rollback are covered by `docs/operations-runbook.md`.
