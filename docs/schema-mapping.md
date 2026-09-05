# Schema redesign — English PascalCase (SQL Server)

Source of truth: [`legacy/DB/Tables`](../legacy/DB/Tables) for DHXE, [`docs/brief.md`](brief.md) for the target product. New database: `Hma`.

Phase 1 product is the brief chain (dispatch → documents → reconcile → monthly statement). Cash receipts, payments, and VAT invoices stay in the schema unused.

## Business tables

| Legacy | New | Notes |
|--------|-----|--------|
| `thanhpho` | `City` | |
| `phongban` | `Department` | |
| `chucvu` | `JobTitle` | |
| `nhanvien` (staff) | `Employee` | Office staff / accountants |
| `nhanvien` (has plate) | `Driver` + `Vehicle` | Split; assigned to Partner `UNASSIGNED` |
| — | `Partner` | New. Seed `UNASSIGNED` |
| `khachhang` | `Customer` | Extra: ContactName, Email, AccountantEmployeeId |
| `tenbg` / `banggia` / `banggia_ct` | `PriceList` / `Revision` / `Item` | Item = pickup city (nullable) + delivery city + vehicle type + unit + surcharge |
| `loaixe` | `VehicleType` | 8 types; preserve Ids |
| `nil` | `DispatchOrder` | See column map |
| `nil_ct` | `DispatchOrderLine` | |
| — | `DispatchDocument` | File metadata; bytes on disk |
| — | `FreightStatement` / `Line` | Monthly statement snapshot |
| — | `ChangeLog` | Audit freight and reconcile |
| `sinhma` | `DocumentSequence` + `SystemParameter` | |
| `congty` | `Company` | |
| `[user]` / `user_form` | `AppUser` / `UserPermission` | |

Dropped: Plexis `ui_*`, `business_object`, `gio`, `phut`, `trongluong`, `RecoveredTab#_1`. Phase 1 does not ETL `phieuthu`, `phieuchi`, `hdgtgt`.

## `nil` → `DispatchOrder`

| Legacy | New |
|--------|-----|
| `nil_ud` | `Code` |
| `ngaylap` + `gio` + `phut` | `CreatedAt` / `PickupAt` |
| `nguoigui_id` | `CustomerId` |
| city of sender / receiver / `hanhtrinh_id` | Location (từ City) → Route 2 điểm + `DispatchOrderStop` |
| `bienso_id` | `DriverId` + plate → `VehicleId` |
| `loaixe_id` | `VehicleTypeId` |
| `cuocdv` | `UnitPrice` |
| `thukhac` | `ExtraCost` |
| `tongthu` | `TotalAmount` |
| `docso` | `AmountInWords` |
| `ghichu` | `Notes` |
| — | `Status` (ETL: Completed), `ReconciliationStatus` (Pending) |

Money: `decimal(20,2)`.
