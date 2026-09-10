# Architecture — Hà Minh Anh (HMA)

Tài liệu này mô tả **thiết kế hệ thống đang có trong source**, không phải roadmap. Nguồn: `src/`, `database/`, `docs/`, `.cursor/rules/`. Cập nhật khi ranh giới lớp, schema, hoặc chuỗi nghiệp vụ thay đổi.

**Sản phẩm:** ứng dụng desktop điều xe và đối soát cước cho Công ty TNHH DV vận tải & TM Hà Minh Anh.  
**Phase hiện tại:** Phase 1 — chuỗi brief: khách hàng → lệnh điều xe → xe/tài xế đối tác → chứng từ → đối soát → bảng kê tháng.  
**Không làm trong phase này:** host web, JWT, Swagger, controller ASP.NET; không compile/copy `legacy/`; không ETL phiếu thu/chi và hóa đơn GTGT.

---



## 1. Mục tiêu nghiệp vụ

Thay Excel/giấy bằng một nguồn dữ liệu khép kín:

```
Khách hàng → Lệnh điều xe → Xe + tài xế → Chuyến → Chứng từ → Cước → Đối soát → Bảng kê tháng
```

Một lệnh điều xe là **dữ liệu gốc**. Không nhập lại cước khi lập bảng kê. Bảng kê chỉ lấy chuyến **đã hoàn thành + đã đối soát + có biên bản giao hàng**.

Năm vấn đề Phase 1 giải quyết: quản lý tập trung, giảm nhầm tuyến/xe/cước, đối soát có checklist, tự động bảng kê tháng, tra cứu theo số lệnh hoặc biển số.

Chi tiết yêu cầu: `[brief.md](brief.md)`. Bản đồ màn hình: `[module-map.md](module-map.md)`.

---



## 2. Stack và solution


| Thành phần  | Chọn                                            |
| ----------- | ----------------------------------------------- |
| Runtime     | .NET 10, C# latest, nullable                    |
| Host        | WPF (`net10.0-windows`)                         |
| Persistence | SQL Server 2016+ / LocalDB; EF Core 10          |
| DI          | `Microsoft.Extensions.Hosting` + constructor DI |
| MVVM        | CommunityToolkit.Mvvm                           |
| PDF / Excel | QuestPDF (Community), ClosedXML                 |
| Test        | xUnit; Application dùng thêm NSubstitute        |


Solution: `src/Hma.slnx`.

```
Hma.Domain
Hma.Application          → Domain
Hma.Infrastructure.SqlServer → Application, Domain
Hma.Reporting            → Application, Domain
Hma.Desktop.Wpf          → Application, Infrastructure, Reporting, Domain
Hma.Domain.Tests         → Domain
Hma.Application.Tests    → Application
```

`legacy/` chỉ để đọc hành vi (VB.NET + Plexis + SQL Server 2000 `DHXE`). Không reference, không copy metadata `ui_*` / Crystal / Infragistics.

---



## 3. Sơ đồ lớp

```
┌─────────────────────────────────────────────────────────────┐
│                    Hma.Desktop.Wpf                          │
│  LoginWindow / MainWindow · Views · ViewModels · Themes     │
│  CommunityToolkit.Mvvm · không SqlClient, không DbContext   │
└──────────────┬──────────────────────────────┬───────────────┘
               │ gọi use case                 │ in / Excel
               ▼                              ▼
┌──────────────────────────┐    ┌─────────────────────────────┐
│     Hma.Application      │    │       Hma.Reporting         │
│  Services, IHmaDbContext │    │  IDocumentPrinter           │
│  Auth, PermissionGuard   │    │  QuestPDF + ClosedXML       │
│  ScreenKeys              │    │  không quy tắc nghiệp vụ    │
└──────────────┬───────────┘    └──────────────▲──────────────┘
               │ phụ thuộc                     │ đọc entity
               ▼                               │
┌──────────────────────────┐                   │
│       Hma.Domain         │───────────────────┘
│  Entities, enums, rules  │
│  VietnameseAmountWords   │
└──────────────▲───────────┘
               │ implement IHmaDbContext
┌──────────────┴───────────┐
│ Hma.Infrastructure       │
│ .SqlServer               │
│  HmaDbContext, Seeder    │
└──────────────────────────┘
```

Hướng phụ thuộc: Desktop và Infrastructure **hướng vào** Application/Domain. Domain không biết EF, WPF, SQL. Application không `System.Windows`, không connection string.

Host web tương lai phải tái sử dụng Application + Domain **không đổi**. Không đưa use case vào ViewModel hay DbContext.

**Lệch thực tế so với lý tưởng:** `Hma.Application` reference `Microsoft.EntityFrameworkCore` để dùng `Include` / `ToListAsync` trên `IQueryable` của `IHmaDbContext`. Đó là adapter query, không phải DbContext concrete. Không thêm repository thứ hai trừ khi interface này không đủ.

---



## 4. Quy tắc ranh giới


| Lớp            | Được                                               | Không được                                                     |
| -------------- | -------------------------------------------------- | -------------------------------------------------------------- |
| Domain         | Entity, enum, rule thuần, đọc số tiền              | EF, WPF, SQL, connection string                                |
| Application    | Use case, `IHmaDbContext`, auth, `PermissionGuard` | `System.Windows`, connection string                            |
| Infrastructure | EF mapping, seed, SQL Server                       | Quy tắc nghiệp vụ mới                                          |
| Reporting      | PDF/Excel từ entity đã tính                        | Tính cước, đối soát, phân quyền                                |
| Desktop        | MVVM, DI host, binding                             | `SqlClient`, `HmaDbContext`, logic nghiệp vụ trong code-behind |


- Thông điệp người dùng: **tiếng Việt**.
- Định danh kỹ thuật: **tiếng Anh** — bảng `Customer`, screen key `dispatch-orders`, sequence `dispatch-order`.
- Kiểm tra quyền tại **biên Application** (`PermissionGuard` trong service), không trong event handler WPF. ViewModel chỉ ẩn/disable nút theo `ICurrentUser.Can`.
- Schema PascalCase số ít, PK `Id`, FK `{Table}Id`. Cột `LegacyId` chỉ phục vụ ETL. Không đưa tên cũ (`nil`, `kh_ud`) vào production.
- Tiền cước Phase 1: `decimal(20,2)`. Phiếu thu/chi/HĐ (đóng băng): `decimal(18,0)`.
- Một type public / một file `.cs` (trừ `partial` WPF và một `DependencyInjection` mỗi project).

---



## 5. Mô hình miền

Mọi entity nghiệp vụ kế thừa `Entity` (`Id`, `LegacyId`), trừ `ChangeLog` và `Company` (không ETL id).

### 5.1 Catalog


| Entity                   | Vai trò                                                                 |
| ------------------------ | ----------------------------------------------------------------------- |
| `City`                   | Tỉnh/thành — địa chỉ khách (`Customer.CityId`)                         |
| `Location`               | Điểm lấy / giao / qua                                                  |
| `Route` + `RouteStop`    | Tuyến = ≥2 điểm có thứ tự; `Fingerprint` unique                        |
| `LocationAlias` / `RouteAlias` | Từ điển Excel: một khóa một đích                                  |
| `Department`, `JobTitle` | Combo nhân viên văn phòng                                               |
| `Employee`               | Nhân viên / kế toán phụ trách khách                                     |
| `Partner`                | Công ty xe. Seed bắt buộc `UNASSIGNED`                                  |
| `Driver`                 | Thuộc một `Partner`                                                     |
| `Vehicle`                | Biển số unique, thuộc `Partner`, optional `VehicleType`                 |
| `VehicleType`            | 8 loại tải (1.25T … 15T), giữ Id khi ETL                                |
| `Customer`               | Bill-to; `AccountantEmployeeId`; `IsWalkIn` cho khách vãng lai `VL-nnn` |
| `PaymentMethod`          | Schema sẵn; Phase 1 không có màn hình                                   |
| `Company`                | Header in (tên công ty)                                                 |


Tách so với legacy: `nhanvien` có biển số → `Driver` + `Vehicle`, gán `Partner UNASSIGNED` rồi gán đối tác thật sau go-live.

### 5.2 Bảng giá

```
PriceList (optional CustomerId, EffectiveFrom/To, HasPriceFluctuation, IsLocked)
  └── PriceListRevision
        └── PriceListItem (Route, VehicleType, UnitPrice, Surcharge)
```

Bảng giá **chung** (`CustomerId` null) hoặc **theo khách**. Cờ `HasPriceFluctuation` trên cả bảng. Khóa: `IsLocked` + `LockedAt` + `LockReason`.

Tra cước `PriceListService.GetFreightAsync(customer, routeId, vehicleType, asOf)` — item còn hiệu lực, revision mới nhất trong cùng bậc:

1. Đúng khách + không biến động
2. Đúng khách + có biến động
3. Bảng chung (ưu tiên không biến động rồi revision mới)
4. Không khớp → cước tay

Cần `RouteId` và `VehicleTypeId`. Khớp cả tuyến (A→B→C), không cộng từng chặng.

### 5.3 Lệnh điều xe (trung tâm)

`DispatchOrder` gắn:

- **Khách thanh toán** `CustomerId` (mặc định = người gửi).
- **Người gửi / người nhận:** FK + snapshot tên, SĐT, địa chỉ, MST (ổn định khi in/bảng kê dù danh mục đổi).
- **Tuyến:** `PickupAt`, địa chỉ, `RouteId` + snapshot `DispatchOrderStop`.
- **Xe / tài xế / loại xe.**
- **Cước:** `UnitPrice + Surcharge + ExtraCost = TotalAmount`; `AmountInWords`.
- **Dòng hàng** `DispatchOrderLine` (tên hàng, kiện, hành trình, km).
- **Chứng từ** `DispatchDocument` (metadata; file trên đĩa).

Trạng thái lệnh `DispatchStatus`: `Draft=0`, `Issued=1`, `Completed=2`, `Locked=3`.  
Đối soát `ReconciliationStatus`: `Pending=0`, `Reconciled=1`.

`CanEdit` = chưa xóa mềm, chưa khóa và chưa đối soát. Không đổi cước sau đối soát. Xóa lệnh = `IsDeleted` (không DROP); vẫn chặn khóa / đã đối soát. Chỉ quản lý khóa lệnh / hủy đối soát. Hủy đối soát bị chặn nếu lệnh đã nằm trên bảng kê.

`RouteLabel` và `HasDeliveryNote` là computed; EF `Ignore`.

### 5.4 Chứng từ file

`DispatchDocumentKind`: `DispatchOrder`, `DeliveryNote`, `Invoice`, `Other`.

File copy vào `{DocumentStorePath}/{orderId:000000}/{timestamp}-{fileName}`.  
`DocumentStorePath` rỗng → `%LocalAppData%\Hma\Documents`. Backup thư mục này cùng SQL.

Đối soát **bắt buộc** có `DeliveryNote`.

### 5.5 Bảng kê tháng

`FreightStatement` unique `(CustomerId, Year, Month)`. Generate lại xóa dòng cũ, giữ header.

`FreightStatementLine` là **snapshot** (mã lệnh, tuyến, biển, tải, tài xế, cước) — không join live khi in.

Tổng: số chuyến, cước, phụ phí, phát sinh, `GrandTotal`, VAT từ `SystemParameter.VatRate` (mặc định 10), `TotalWithVat`.

Điều kiện vào bảng kê (cả ba): `Status == Completed`, `ReconciliationStatus == Reconciled`, có chứng từ `DeliveryNote`.

### 5.6 Audit, số chứng từ, tham số

`ChangeLog`: `EntityName` + `EntityId`, `Action`, `Summary`, `OldJson`/`NewJson`, user, thời điểm. Lệnh ghi: Create, UpdateFreight, UpdateVehicleDriver, UpdateRoute, UpdateStatus, Lock, Reconcile, Unreconcile.

`DocumentSequence.Key` → số tăng, format `000`: `dispatch-order`, `freight-statement`, `walk-in-customer`, và (đóng băng) `cash-receipt`, `cash-payment`, `vat-invoice`.

`SystemParameter`: `VatRate`, `DocumentStorePath`, `SchemaVersion`.

### 5.7 Bảng đóng băng (schema + service + view, **không** nav Phase 1)

`CashReceipt` / `CashPayment` / `VatInvoice` + `VatInvoiceLine`. Giữ để phase kế toán; ETL `04_cash.sql` / `05_invoices.sql` **không chạy** lúc cutover.

---



## 6. Luồng nghiệp vụ cốt lõi



### 6.1 Tạo và sửa lệnh

1. User có `Create` trên `dispatch-orders` → lệnh mới `Issued`, `CreatedByUserId`, một dòng hàng trống.
2. Chọn người gửi (bắt buộc cho bill-to), người nhận, xe, tài xế.
3. `ApplyFreightAsync` đổ đơn giá/phụ phí từ bảng giá; user có thể sửa thêm phát sinh.
4. `SaveAsync`: snapshot gửi/nhận, `DispatchOrderRules`, không sửa lệnh khóa (trừ manager), không đổi cước nếu đã đối soát, lấy `VehicleType` từ xe nếu thiếu, `RecalculateTotal` + đọc tiền, cấp số nếu mới, thay toàn bộ dòng hàng, ghi ChangeLog.
5. Khách vãng lai: `CreateWalkInAsync` → `VL-{seq}`, `IsWalkIn`.

In một lệnh / theo ngày / tháng / theo khách và Excel danh sách cước đi qua `IDocumentPrinter`.

### 6.2 Đối soát

Màn `reconcile` lọc lệnh `Completed` + `Pending`. Checklist: biên bản, tuyến, đơn giá, phụ phí, phát sinh, tổng, trạng thái. Lịch sử ChangeLog bên cạnh.

`ReconcileAsync`: phải Completed, phải có DeliveryNote, ghi `ReconciledAt` / `ReconciledByUserId`.  
`UnreconcileAsync`: chỉ manager; fail nếu đã có `FreightStatementLine`.

### 6.3 Bảng kê

Chọn khách + tháng/năm → `FreightStatementService.GenerateAsync` (quyền `Create` trên `statements`) → PDF/Excel.

### 6.4 Dashboard và báo cáo

KPI tháng (`DashboardQueryService.MonthAsync`): tổng cước, số chuyến, chưa/đã đối soát, chờ chứng từ (thiếu DeliveryNote). Lưới theo khách / theo xe trong khoảng ngày.

`ReportQueryService`: lệnh theo ngày hoặc khoảng (optional khách). In PDF tổng hợp. Preset tuần/quý/năm nằm ở ViewModel.

### 6.5 Tra cứu

Ô số lệnh **hoặc** biển số → `DispatchOrderService.SearchAsync` (tối đa 500, mới nhất trước) → mở workspace lệnh qua `IWorkspaceNavigator`.

Lịch sử chuyến cũng có trên màn tài xế / xe (`CatalogService.TripsByDriverAsync` / `TripsByVehicleAsync`).

---



## 7. Application — use case

Đăng ký trong `Hma.Application/DependencyInjection.cs`. Mọi service mutating (trừ cash/VAT đóng băng) gọi `PermissionGuard`.


| Service                                     | Trách nhiệm                                                   |
| ------------------------------------------- | ------------------------------------------------------------- |
| `AuthService`                               | Login, nạp permission + employee, gán `ICurrentUser`          |
| `CurrentUser`                               | Singleton phiên; `IsManager` bypass mọi `Can`                 |
| `Pbkdf2PasswordHasher`                      | PBKDF2 100k SHA256; vẫn nhận `RESET:` từ ETL                  |
| `PermissionGuard`                           | Ném `InvalidOperationException` tiếng Việt nếu thiếu quyền    |
| `UserAdminService`                          | CRUD user + matrix quyền; màn `users`                         |
| `CustomerService`                           | Tìm/sửa/xóa; unique `Code`                                    |
| `CatalogService`                            | City, phòng ban, chức vụ, NV, đối tác, TX, xe; lịch sử chuyến |
| `PriceListService`                          | Header/revision/item; `GetFreightAsync`                       |
| `DispatchOrderService`                      | Tìm, CRUD, cước, khóa, status, đối soát, walk-in              |
| `DispatchDocumentService`                   | Copy file, metadata, xóa file                                 |
| `FreightStatementService`                   | List/get/generate tháng                                       |
| `DashboardQueryService`                     | KPI + group khách/xe                                          |
| `ReportQueryService`                        | Lệnh theo ngày/kỳ (và phiếu chi — đóng băng)                  |
| `ChangeLogService`                          | Ghi/đọc audit                                                 |
| `DocumentNumberService`                     | Tăng `LastValue`                                              |
| `SettingsService`                           | VAT, đường chứng từ, số đếm LDX/bảng kê                       |
| `CompanyService`                            | Header in; Save thông tin công ty (quyền `settings`)          |
| `LocationService` / `RouteService` | Catalog điểm / tuyến |
| `LocationAliasService` / `RouteAliasService` / `CustomerAliasService` | Từ điển điểm / tuyến / khách trong hub Cấu hình |
| `CashDocumentService` / `VatInvoiceService` | Có code; **không** gắn nav Phase 1                            |


`IHmaDbContext`: `IQueryable<T>` cho từng DbSet, `Add`/`Update`/`Remove`/`FindAsync`/`SaveChangesAsync`. Schema: `HmaDatabaseInitializer.MigrateAndSeedAsync` (Infrastructure).

Lỗi nghiệp vụ: `InvalidOperationException` với câu tiếng Việt. Không dùng exception làm luồng bình thường.

---



## 8. Persistence



### 8.1 Schema

Chi tiết cột, FK, index, enum, snapshot, và khác biệt SQL/EF: [`Database.md`](Database.md).

Nguồn sự thật schema: code-first — entity + `OnModelCreating` + `Migrations/`. [`database/001_schema.sql`](../database/001_schema.sql) cho cutover/ETL. Seed SQL: [`002_seed.sql`](../database/002_seed.sql). [`003_brief_schema.sql`](../database/003_brief_schema.sql) chỉ cảnh báo schema cũ; **không** migrate tại chỗ.

EF: `HmaDbContext` map 1–1 tên bảng PascalCase, precision tiền, unique index (Partner.Code, Driver.Code, Vehicle.PlateNumber, UserPermission, FreightStatement kỳ), `DeleteBehavior.Restrict` trên FK nhiều nhánh Customer/City/User của lệnh. `RowVersion` (`IsRowVersion`) trên lệnh, bảng giá, bảng kê, dãy số. `IHmaDbContext.ApplyOriginalRowVersion` gắn token lúc mở form khi `Update` từ ViewModel.

Dev LocalDB: nếu thiếu bảng `Partner` hoặc cột `DispatchOrder.SenderCustomerId` / `SenderName` / `RowVersion` / `IsDeleted` → **drop + recreate** rồi seed. Cờ `--seed` chỉ tạo DB rồi thoát.

File chứng từ: `IFileStorage` / `LocalDiskFileStorage`. `DispatchDocument.StoredPath` là key tương đối. Lệnh đã xóa: `IsDeleted` + query filter (search / dashboard / bảng kê không thấy).

Mapping legacy → mới: [`schema-mapping.md`](schema-mapping.md).

### 8.2 Seed runtime (`DatabaseSeeder`)

- 9 `VehicleType` (gồm `1.5T`; giữ `1.45T` cho ETL)  
- Partner `UNASSIGNED`  
- 17 `AppScreen` (không gồm cash/VAT)  
- Sequences + `VatRate=10`, `DocumentStorePath=""`, `SchemaVersion=legacy-ux-1`  
- Company Hà Minh Anh  
- User `admin` / `admin123` (`IsManager`)  
- User `ketoan` / `ketoan123`: View/Create/Update/Print trên catalog + lệnh + đối soát + bảng kê + tra cứu; dashboard/reports chỉ View+Print; không Delete; không `users`/`settings`
- `DemoDataSeeder`: nếu **chưa có khách hàng** thì nạp danh mục từ bảng điều xe 11/08/2026 (khách, tài xế, xe, điểm, tuyến, bí danh). Không seed lệnh / bảng giá / bảng kê. Không chạy khi DB đã có khách (ETL/production). Xóa khách hoặc CSDL rồi mở app để nạp lại.

Chuỗi kết nối: `Hma.Desktop.Wpf/appsettings.json` → `ConnectionStrings:Hma`.

---



## 9. Phân quyền

Mô hình port từ `user_form` / `Return_PQ`:

`PermissionAction`: Create=1, Delete=2, Update=3, View=4, Print=5.

`UserPermission` per (`AppUser`, `AppScreen`). **Manager (**`IsManager`**) bỏ qua matrix.** Ẩn nav không đủ: nút Lưu/Xóa/In/Khóa bind `CanCreate`/`CanUpdate`/`CanDelete`/`CanPrint` từ `WorkspaceBase`.


| Vai trò | Nav                                                           | Ghi chú                                  |
| ------- | ------------------------------------------------------------- | ---------------------------------------- |
| Kế toán | Catalog, lệnh, tra cứu, đối soát, bảng kê, dashboard, báo cáo | Seed không cho xóa; không users; Cấu hình hiện nếu có `cities` hoặc `settings` |
| Quản lý | Tất cả trên + users + settings                                | Khóa lệnh, khóa bảng giá, hủy đối soát   |


`AppScreen.Key` / `ScreenKeys` không đổi: `dashboard`, `customers`, `partners`, `drivers`, `vehicles`, `employees`, `departments`, `job-titles`, `cities`, `locations`, `routes`, `price-lists`, `dispatch-orders`, `dispatch-grid-edit`, `lookup`, `reconcile`, `statements`, `reports`, `settings`, `users`.

Main nav **không** liệt kê từng key đó. Shell gom: **Tuyến đường** (`WorkspaceKeys.RouteCatalog` = `route-catalog`, không phải AppScreen — hiện nếu manager hoặc `Can(locations)` / `Can(routes)`), **Lệnh điều xe** (hub form / sửa theo khách / nhập Excel; `dispatch-grid-edit` không còn item riêng), **Cấu hình** (tham số, công ty, thành phố, người dùng, từ điển — hiện nếu manager hoặc `Can(settings)` hoặc `Can(cities)`). Tra cứu / đối soát `OpenDispatch(id)` → hub form → `OpenByIdAsync`.

Đóng băng (có view/VM, **không** thêm vào `MainViewModel.Items`): phiếu thu, phiếu chi, HĐ GTGT.

---



## 10. Desktop (WPF MVVM)



### 10.1 Host

`App.OnStartup`:

1. `Host` + `appsettings.json`
2. `AddHmaApplication` / `AddHmaInfrastructure` / `AddHmaReporting`
3. `HmaDatabaseInitializer.MigrateAndSeedAsync`
4. `LoginWindow` (scope riêng)
5. Scope phiên → `MainWindow`
6. `ICurrentUser` **singleton** sống suốt process sau login

Lỗi UI: `DispatcherUnhandledException` → MessageBox tiếng Việt, không crash.

### 10.2 Shell

`MainWindow`: nav trái (lọc theo View/Create, cộng quy tắc hub Cấu hình / Tuyến đường), `ContentControl` + `DataTemplate` theo kiểu ViewModel.

`MainViewModel` giữ workspace scoped của **item nav**. Hub `SettingsWorkspaceViewModel` / `RouteCatalogWorkspaceViewModel` / `DispatchHubWorkspaceViewModel` bọc view lồng (City, User, Location, Route, form lệnh, lưới theo khách, nhập Excel). `WorkspaceNavigator.OpenDispatch` chọn nav lệnh và `DispatchHubWorkspaceViewModel.OpenOrderAsync`.

### 10.3 Workspace

Một màn catalog = `FooView.xaml` + code-behind tối thiểu + `FooWorkspaceViewModel`. Hub (Cấu hình, Tuyến đường, Lệnh điều xe) là một ViewModel vỏ + section ListBox, host view con; `AppScreen.Key` vẫn gắn view con.

`WorkspaceBase`: mode Browse/Create/Edit, overlay editor, toast, `RunAsync` bắt exception → toast lỗi, `UsePermissions(screenKey)` cho enable nút.

UX:

- Catalog: **tìm trái — form phải** (hoặc overlay).  
- Lệnh: **lưới trên — form dưới**.  
- Pattern legacy: Tìm → lưới → xem / double-click → chi tiết.

In: ViewModel lấy `Company` + entity đã load, gọi `IDocumentPrinter`, mở file temp. Không Crystal.

Theme: `Themes/Fields.xaml`, `Typography.xaml`. Converter validation: `FirstValidationErrorConverter`. Rule nhập (`InputRuleKind`) dùng chung Domain cho SĐT / email / MST / tiền.

---



## 11. Reporting

`IDocumentPrinter` / `DocumentPrinter` — adapter, không rule:


| API                                                                            | Đầu ra                       |
| ------------------------------------------------------------------------------ | ---------------------------- |
| `PrintDispatch`                                                                | PDF lệnh A4                  |
| `PrintDispatchSummary` / `PrintDailyDispatch`                                  | PDF landscape danh sách lệnh |
| `PrintFreightStatement`                                                        | PDF bảng kê tháng            |
| `ExportFreightStatementExcel`                                                  | Excel bảng kê + VAT          |
| `ExportCustomersExcel` / `ExportDispatchExcel`                                 | Danh sách KH / cước          |
| `PrintCashReceipt` / `PrintCashPayment` / `PrintVatInvoice` / `ExportVatExcel` | Có sẵn; nav đóng băng        |


Tiền bằng chữ: Domain `VietnameseAmountWords` (port `Nil.Utility.DocSo`) qua `AmountText.From`.

---



## 12. Domain rules và test

Rules tĩnh, ném tiếng Việt:


| Type                                                               | Kiểm                                    |
| ------------------------------------------------------------------ | --------------------------------------- |
| `CustomerRules` / `PartnerRules` / `DriverRules` / `EmployeeRules` | Mã/tên bắt buộc; SĐT/email/MST optional |
| `DispatchOrderRules`                                               | Cước ≥ 0; SĐT/MST gửi-nhận              |
| `PriceListItemRules`                                               | Điểm giao, loại xe, cước ≥ 0            |
| `PhoneRules`                                                       | `0…` hoặc `+84`                         |
| `EmailRules`                                                       | Định dạng email                         |
| `TaxCodeRules`                                                     | 10 hoặc 13 số                           |
| `MoneyRules`                                                       | Parse, không âm / phải dương            |
| `InputRuleEvaluator`                                               | Flag cho binding UI                     |


Test khóa hành vi (không test UI):

- **Domain:** SĐT, email, MST, tiền, customer/dispatch rules, đọc số, `InputRuleEvaluator`  
- **Application:** `PermissionGuard`, `AmountText`

Thêm test mới cạnh hành vi: cước, sequence, đọc số, quyền.

---



## 13. ETL và cutover

Legacy `DHXE` là SQL Server 2000 — **không attach** được LocalDB hiện đại. Cần instance nguồn tương thích + target SQL 2016+/LocalDB.

Kế hoạch downtime: `[etl-cutover.md](etl-cutover.md)`. Inventory form/SP: `[legacy-inventory.md](legacy-inventory.md)`.

Thứ tự script:


| Script                            | Việc                                                                                                              |
| --------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| `etl/00_precheck.sql`             | Đếm nguồn                                                                                                         |
| `001_schema.sql` + `002_seed.sql` | Tạo `Hma`                                                                                                         |
| `etl/01_master.sql`               | City, org, Employee, Customer, loaixe, UNASSIGNED, Driver/Vehicle từ biển, Company                                |
| `etl/02_pricing.sql`              | Unpivot `banggia_ct` → `PriceListItem`                                                                            |
| `etl/03_dispatch.sql`             | `nil` → lệnh (Completed, Pending); `nguoigui` → Customer + Sender snapshot; `nguoinhan` → Receiver; hàng `nil_ct` |
| `etl/06_security.sql`             | User; password `RESET:` (đổi sau go-live)                                                                         |
| `etl/07_sequences.sql`            | `nil_ud` + VAT                                                                                                    |
| `etl/99_validate.sql`             | Count KH/lệnh; `SUM(tongthu)` vs `SUM(TotalAmount)`; FK mồ côi                                                    |


**Không chạy** `04_cash.sql`, `05_invoices.sql` ở Phase 1. Không dual-write. Rollback = restore `DHXE`, mở lại PCMNavigator.

---



## 14. Thành phần Desktop (checklist)


| Screen key        | View                            | ViewModel                    | Application                                   |
| ----------------- | ------------------------------- | ---------------------------- | --------------------------------------------- |
| `dashboard`       | DashboardView                   | DashboardWorkspaceViewModel  | DashboardQueryService                         |
| `customers`       | CustomerView                    | CustomerWorkspaceViewModel   | CustomerService                               |
| `partners`        | PartnerView                     | PartnerWorkspaceViewModel    | CatalogService                                |
| `drivers`         | DriverView                      | DriverWorkspaceViewModel     | CatalogService                                |
| `vehicles`        | VehicleView                     | VehicleWorkspaceViewModel    | CatalogService                                |
| `employees`       | EmployeeView                    | EmployeeWorkspaceViewModel   | CatalogService                                |
| `departments`     | DepartmentView                  | DepartmentWorkspaceViewModel | CatalogService                                |
| `job-titles`      | JobTitleView                    | JobTitleWorkspaceViewModel   | CatalogService                                |
| `cities`          | CityView (hub Cấu hình)     | CityWorkspaceViewModel       | CatalogService                                |
| `locations`       | LocationView (hub Tuyến đường) | LocationWorkspaceViewModel | LocationService                               |
| `routes`          | RouteView (hub Tuyến đường) | RouteWorkspaceViewModel      | RouteService                                  |
| `price-lists`     | PriceListView                   | PriceListWorkspaceViewModel  | PriceListService                              |
| `dispatch-orders` | DispatchHubView → DispatchView / DispatchImportView | DispatchHubWorkspaceViewModel, DispatchWorkspaceViewModel, DispatchImportWorkspaceViewModel | DispatchOrderService, DispatchDocumentService, DispatchImportService |
| `dispatch-grid-edit` | DispatchGridEditView (hub lệnh) | DispatchGridEditWorkspaceViewModel | DispatchOrderService                    |
| `lookup`          | LookupView                      | LookupWorkspaceViewModel     | DispatchOrderService                          |
| `reconcile`       | ReconcileView                   | ReconcileWorkspaceViewModel  | DispatchOrderService, ChangeLogService        |
| `statements`      | StatementView                   | StatementWorkspaceViewModel  | FreightStatementService                       |
| `reports`         | ReportView                      | ReportWorkspaceViewModel     | ReportQueryService, DashboardQueryService     |
| `settings`        | SettingsView                    | SettingsWorkspaceViewModel   | SettingsService, CompanyService, LocationAliasService, RouteAliasService, CustomerAliasService |
| `users`           | UserView (hub Cấu hình)         | UserWorkspaceViewModel       | UserAdminService                              |
| *(đóng băng)*     | CashReceipt/Payment/InvoiceView | *WorkspaceViewModel          | CashDocumentService, VatInvoiceService        |


---



## 15. Chạy và vận hành

```text
dotnet run --project src/Hma.Desktop.Wpf
```

Lần đầu tạo database `Hma` và seed. Đăng nhập: `admin` / `admin123` hoặc `ketoan` / `ketoan123`.

Schema production: chạy `001_schema.sql` + `002_seed.sql` (và ETL nếu cutover), không dựa vào `EnsureDeleted` của app.

Sau go-live: gán xe/TX khỏi `UNASSIGNED`, reset mật khẩu ETL, backup `DocumentStorePath`.

---



## 16. Việc cố ý chưa làm

- Host web / API / JWT / Swagger  
- Nav và ETL phiếu thu, phiếu chi, HĐ GTGT  
- Repository song song với `IHmaDbContext`  
- Lazy loading EF  
- Port Plexis metadata, Crystal, Infragistics  
- Mẫu bảng kê khác nhau theo từng khách (brief cho phép sau)

Phase kế toán sau này: bật nav cash/VAT, chạy ETL 04/05, siết `PermissionGuard` trên `CashDocumentService` / `VatInvoiceService` — **không** nhét rule vào WPF.

---



## 17. Tài liệu cùng bộ


| File                                         | Nội dung                                |
| -------------------------------------------- | --------------------------------------- |
| [`brief.md`](brief.md)                       | Yêu cầu nghiệp vụ Phase 1               |
| [`module-map.md`](module-map.md)             | Screen key ↔ view ↔ use case            |
| [`Database.md`](Database.md)                 | Thiết kế database Hma (cột, FK, enum)   |
| [`hardening-plan.md`](hardening-plan.md)     | Review kiến trúc & kế hoạch sửa Phase 1 |
| [`schema-mapping.md`](schema-mapping.md)     | DHXE → Hma                              |
| `[etl-cutover.md](etl-cutover.md)`           | Cutover ≤ 1 ngày                        |
| `[legacy-inventory.md](legacy-inventory.md)` | Form, SP, report cũ                     |
| `[../README.md](../README.md)`               | Chạy app, layout repo                   |
| `.cursor/rules/hma-architecture.mdc`         | Ràng buộc lớp (luôn apply khi sửa code) |


