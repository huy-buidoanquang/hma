# ETL cutover plan (downtime ≤ 1 day)

Legacy DB is SQL Server 2000 (`DHXE`). It cannot attach to current LocalDB. Cutover requires a compatible source engine and SQL Server 2016+ / LocalDB as **target** (`Hma`).

Phase 1 migrates catalogs, price lists, and dispatch orders into the brief schema. It does **not** migrate phiếu thu/chi or HĐ GTGT.

## Preconditions
1. Dry-run ETL ≥ 2 times on a restored copy; record duration and validation diffs.
2. New app smoke-tested against seeded `Hma`.
3. Full backup of `DHXE` immediately before freeze.
4. File share / `DocumentStorePath` ready (empty at go-live).

## Cutover day

```
T+0h00  Stop PCMNavigator; disable SQL logins except ETL
T+0h15  Full backup DHXE; run 00_precheck.sql
T+0h30  CREATE DATABASE Hma; run database/001_schema.sql + 002_seed.sql
T+1h00  Run ETL 01_master, 02_pricing, 03_dispatch, 06_security, 07_sequences
T+3h00  Run 99_validate.sql; spot-check 20–50 lệnh
T+4h00  Point desktop connection string to Hma; smoke test
T+5h00  Go-live OR rollback (restore DHXE, reopen legacy)
```

Do not run `04_cash.sql` or `05_invoices.sql` in Phase 1.

## Script order
- `00_precheck.sql` — source counts
- `01_master.sql` — City, org, Employee, Customer, loaixe, Partner UNASSIGNED, Driver/Vehicle from plates, Company
- `02_pricing.sql` — PriceList + unpivot banggia_ct into route items
- `03_dispatch.sql` — DispatchOrder from `ngaylap`/`cuocdv`/`bienso_id`/`hanhtrinh_id`
- `06_security.sql` — users (`RESET:` passwords)
- `07_sequences.sql` — `nil_ud` + VAT rate
- `99_validate.sql` — counts, `SUM(tongthu)` vs `SUM(TotalAmount)`, orphan Customer FK

## Validation
- Count(Customer) = count(khachhang)
- Count(DispatchOrder) = count(nil)
- SUM(DispatchOrder.TotalAmount) = SUM(nil.tongthu)
- Drivers/vehicles created for every distinct `nhanvien.biensoxe`
- Latest dispatch sequence matches `sinhma.nil_ud`

## Rollback
1. Stop new app.
2. Restore `DHXE` backup taken at freeze.
3. Re-enable legacy logins; start `PCMNavigator`.

## After go-live
- Assign vehicles/drivers from `UNASSIGNED` to real partners.
- Reset AppUser passwords.
- No dual-write.
- Backup `DocumentStorePath` with SQL backups.
