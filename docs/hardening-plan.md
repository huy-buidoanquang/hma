# Review feedback & kế hoạch sửa — HMA Phase 1

> Trạng thái triển khai 11/09/2026: P0/P1 trong tài liệu này đã được hiện thực hóa bằng migration, workflow maker–checker, storage abstraction, soft-delete lệnh, pricing traceability, partner commercial/settlement, transport exception, CI và integration test SQL Server. Không migration nào đã được tự động áp lên database production.
> Các mục “hiện trạng” bên dưới là baseline trước hardening, được giữ lại để giải thích nguyên nhân và quyết định kiến trúc; trạng thái sau triển khai là `Architecture.md` và `Database.md`.

Đối chiếu từng nhận xét kiến trúc với **code đang chạy** (`src/`, `database/001_schema.sql`), không với ý định. Kết luận: làm gì ngay, làm gì sau, **không** làm gì vì sai chỗ hoặc vượt Phase 1.

---

## 0. Kết luận ngắn

Ba điểm reviewer xếp “bắt buộc trước khi code” **không đồng đều**:

| Đề xuất reviewer | Kết luận | Lý do (rút gọn) |
|------------------|----------|-----------------|
| `RowVersion` / optimistic concurrency | **Làm ngay (P0)** | Lost update trên lệnh là bug thật; schema và `SaveAsync` hiện last-write-wins |
| Bọc file bằng `IFileStorage` | **Làm ngay (P0)** | `DispatchDocumentService` đang `File.Copy` / `%LocalAppData%` — rẻ, đúng ranh giới lớp |
| Thay `ChangeLog` bằng Temporal Tables | **Không thay** | `ChangeLog` là nhật ký sự kiện nghiệp vụ (đối soát UI), không phải row history; Temporal không thay được “ai đối soát / đổi cước” |
| MediatR / CQRS để “chuẩn 3-tier” | **Không làm Phase 1** | MediatR không dời connection string khỏi client; 3-tier thật là API host giữ SQL |
| Soft delete mọi entity | **Không làm hàng loạt** | Restrict FK đã chặn xóa master đang dùng; soft-delete toàn bộ vỡ unique `Code` và ETL |
| `IPricingStrategy` | **Không làm lúc một công thức** | `GetFreightAsync` đã cô lập; Strategy khi có khách thứ hai |

Ngoài reviewer: `DocumentSequence.NextAsync` đua số lệnh, và `appsettings.json` chứa mật khẩu SQL trên máy client — cùng gốc rủi ro 2-tier, xử lý trong P0/P1.

---

## 1. Điểm mạnh — xác nhận (không sửa)

Đúng với repo:

- Domain không reference EF/WPF. Application không `System.Windows`. Desktop không `HmaDbContext` / `SqlClient`.
- Chuỗi lệnh → chứng từ → đối soát → bảng kê nằm trong `DispatchOrderService` / `FreightStatementService`, không trong View.
- Snapshot gửi/nhận trên `DispatchOrder` và snapshot dòng trên `FreightStatementLine` — cước lịch sử không bị danh mục mới ghi đè.
- Quyền kiểm ở Application (`PermissionGuard`), không ở click handler.

Giữ nguyên. Không “enterprise-ify” các phần này.

---

## 2. Từng đề xuất — suy luận chặt

### 2.1 Topology 2-tier và MediatR

**Hiện trạng (đúng như reviewer):**

- `Hma.Desktop.Wpf/appsettings.json` chứa `ConnectionStrings:Hma` (SQL auth).
- `App.xaml.cs` gọi `AddHmaInfrastructure(cs)` rồi `EnsureCreatedAndSeededAsync`.
- ViewModel gọi thẳng `DispatchOrderService.SaveAsync` — không MediatR, không HTTP.

Đây là **2-tier**: process WPF = client + application + connection. Clean Architecture **không** biến nó thành 3-tier. Rủi ro bảo mật connection string trên máy kế toán là thật nếu mở SQL ra WAN.

**MediatR không giải quyết rủi ro đó.**  
`IRequest<SaveDispatch>` vẫn chạy trong process WPF, vẫn dùng `IHmaDbContext` local. Connection string vẫn trên client. Khi lên Web, bọc API quanh **cùng service** (`DispatchOrderService`) cũng xong — không cần đổi toàn bộ thành Command/Query class (mỗi type một file theo rule repo → bùng nổ file).

Kiến trúc Phase 2 đã ghi: host web tái sử dụng Application + Domain, **không** thêm controller vào solution hiện tại. Việc cần làm lúc lên 3-tier:

1. Project host ASP.NET (hoặc gRPC) đăng ký `AddHmaApplication` + `AddHmaInfrastructure`; connection string chỉ trên server.
2. WPF đổi sang HTTP client; xóa connection string khỏi máy user.
3. SQL chỉ listen nội bộ / VPN — không mở port Internet.

**Quyết định:** không thêm MediatR trong Phase 1. Ghi rõ lộ trình 3-tier ở mục 5 (Phase 2), không giả vờ MediatR là bước bắt buộc.

**P1 liên quan bảo mật (không phải MediatR):** không commit mật khẩu SA; dùng Windows auth hoặc login SQL quyền hẹp; `appsettings.json` local / User Secrets. Không nằm trong 3 mục reviewer nhưng cùng gốc 2.1.

---

### 2.2 Concurrency — **chấp nhận, bắt buộc**

**Hiện trạng:** không có `rowversion` / `Timestamp` trên bất kỳ bảng nào. `DispatchOrderService.SaveAsync` đọc `AsNoTracking`, rồi `db.Update(order)` + `SaveChangesAsync` — **last write wins**.

Hai máy mở cùng lệnh:

1. Điều phối sửa biển số, lưu.  
2. Kế toán sửa cước trên bản đã load trước, lưu → ghi đè xe, không báo.

Cùng pattern: `LockAsync`, `SetStatusAsync`, `ReconcileAsync`, `UnreconcileAsync` (Find → sửa → Update). Hai kế toán đối soát / hủy đối soát: một người thắng im lặng.

`DocumentNumberService.NextAsync`: đọc `LastValue`, `++`, Save — hai lệnh tạo cùng lúc có thể **trùng số** (`DispatchOrder.Code` không unique SQL).

`FreightStatementService.GenerateAsync`: unique `(CustomerId, Year, Month)` chặn insert kép; nhánh “đã có header” hai người generate cùng kỳ → một người xóa dòng của người kia.

**Quyết định:** optimistic concurrency bằng `rowversion` SQL Server.

Bảng **bắt buộc Phase 1:**

| Bảng | Vì sao |
|------|--------|
| `DispatchOrder` | Lost update cước / xe / trạng thái / đối soát |
| `DocumentSequence` | Đua số chứng từ |
| `FreightStatement` | Generate lại kỳ đang mở |
| `PriceList` | Khóa bảng giá vs sửa dòng |

Không cần RowVersion mọi catalog (City, Department) ở P0 — xung đột thấp, Restrict đủ.

Hành vi UX: `DbUpdateConcurrencyException` → `InvalidOperationException("Dữ liệu đã bị người khác thay đổi. Tải lại rồi lưu lại.")`. `WorkspaceBase.RunAsync` đã bắt exception → toast. Sau lỗi: reload entity (ViewModel `OpenAsync` / `LoadAsync`), không đè im lặng.

`NextAsync`: bắt concurrency, retry 3 lần tăng `LastValue` — không để user thấy lỗi số chứng từ trừ khi hết retry.

---

### 2.3 File storage — **chấp nhận, P0**

**Hiện trạng:** `DispatchDocumentService` trong Application:

- `Environment.GetFolderPath(LocalApplicationData)`
- `Directory.CreateDirectory`, `File.Copy`, `File.Delete`, `File.Exists`
- `StoredPath` = path tuyệt đối máy local

Application đang phụ thuộc filesystem Windows. `%LocalAppData%` trên mỗi PC = chứng từ **không dùng chung** nếu nhiều máy — tệ hơn network share mà reviewer lo. Settings đã có `DocumentStorePath` (share) nhưng default rỗng → local.

**Quyết định:** interface ở Application, implement disk ở Infrastructure. Không S3/MinIO Phase 1.

```
IFileStorage
  SaveAsync(relativeKey, stream) → stored key
  OpenReadAsync(key)
  DeleteAsync(key)
  ExistsAsync(key)
```

- `DispatchDocument.StoredPath` đổi nghĩa thành **storage key** (ví dụ `000123/20260827120000-bbgh.pdf`), không phải `C:\Users\...`.
- `LocalDiskFileStorage`: root = `DocumentStorePath` hoặc `%LocalAppData%\Hma\Documents`.
- WPF `OpenFileDialog` → stream → `AttachAsync`; service không `File.Copy`.
- Phase 2: `S3FileStorage` cùng interface; không sửa `DispatchOrderService`.

Reviewer gọi `IStorageProvider` / `IFileStorageService` — tên repo: `IFileStorage` (Application/Abstractions).

---

### 2.4 Temporal Tables vs ChangeLog — **không thay ChangeLog**

Reviewer: JSON chậm khi query SQL; Temporal “không tốn 1 dòng C#”.

**Hiện trạng thật:**

- `ChangeLog` index `(EntityName, EntityId)` — `ForEntityAsync` **không** parse JSON trong SQL.
- Màn đối soát bind `History`: `Action` + `Summary` tiếng Việt + user (`ChangeLog.UserId`).
- Sự kiện có tên: `Create`, `UpdateFreight`, `UpdateVehicleDriver`, `UpdateRoute`, `UpdateStatus`, `Lock`, `Reconcile`, `Unreconcile`.

Temporal Tables lưu **mọi phiên bản dòng**, không lưu:

- ý nghĩa “Đã đối soát” / “Đổi cước”;
- user trừ khi thêm `UpdatedByUserId` lên chính `DispatchOrder` (mọi cột period).

Hai người cùng Reconcile: Temporal cho hai hàng history; UI kế toán hiện cần timeline sự kiện, không `TemporalAsOf`.

Chi phí Temporal trên HMA:

- `PERIOD FOR SYSTEM_TIME`, history table, `SYSTEM_VERSIONING = ON`.
- ETL `IDENTITY_INSERT` vào bảng temporal: tắt versioning, insert, bật lại — `03_dispatch.sql` phức tạp.
- `EnsureCreated` vs `001_schema.sql` vốn đã lệch; Temporal làm lệch thêm.
- EF `TemporalAsOf` không thay `ForEntityAsync` trên màn reconcile.

Hệ thống kế toán lớn thường **cả hai**: row versioning **và** nhật ký nghiệp vụ. Reviewer đề “bỏ ChangeLog” là **thay nhầm công cụ**.

**Quyết định:** giữ `ChangeLog`. Không bật Temporal Phase 1. Phase 2 (tùy chọn, **cộng** không thay): Temporal trên `DispatchOrder` nếu cần “lệnh lúc 15h ngày 1/8”; UI đối soát vẫn đọc `ChangeLog`.

JSON `OldJson`/`NewJson` chỉ để hiển thị diff — chấp nhận. Không query JSON trong SQL.

---

### 2.5 Soft delete — **không ISoftDelete toàn hệ; có P1 hẹp**

**Hiện trạng:** hard delete `Customer`, catalog, `DispatchOrder` (nếu chưa khóa / chưa đối soát), `DispatchDocument`, `PriceListItem`. FK lệnh → khách/xe/TX: **Restrict** — xóa khách đang có lệnh fail SQL (thường lỗi tiếng Anh thô).

`ChangeLog.EntityId` **không phải FK**. Xóa lệnh không vỡ FK log; log trỏ Id đã mất — reviewer đúng về “tham chiếu ngữ nghĩa”, sai nếu nghĩ CASCADE xóa ChangeLog.

Quy tắc “không hard delete entity đã có Id” quá nặng cho catalog nháp (gõ nhầm thành phố). Unique `Code` + `IsDeleted`: cần **filtered unique index** (`WHERE IsDeleted = 0`) nếu muốn tái sử dụng mã — ETL và `001_schema` phải đổi đồng bộ.

**Quyết định:**

- **Không** `ISoftDelete` trên mọi entity Phase 1.
- **P1:** `DispatchOrder.IsDeleted BIT NOT NULL DEFAULT 0` + query filter EF + `DeleteAsync` chỉ set flag (vẫn chặn Locked/Reconciled). Mã lệnh không tái cấp. Dòng hàng / chứng từ giữ nguyên (CASCADE không chạy).
- Catalog: giữ hard delete; **P1:** bắt `DbUpdateException` (FK) → `"Không xóa được vì đang được lệnh điều xe hoặc danh mục khác sử dụng."`
- `UNASSIGNED` / `AppScreen` / sequence: không bao giờ xóa từ UI (đã vậy).

---

### 2.6 Pricing Strategy — **không làm khi một công thức**

`PriceListService.GetFreightAsync` (~35 dòng) đã là điểm duy nhất tra cước; `DispatchOrderService.ApplyFreightAsync` chỉ gọi nó. Brief HMA: tuyến × loại xe × khách/chung — **đúng schema hiện tại**.

Strategy với một `StandardPricingStrategy` = thêm file/DI, không đổi hành vi. Làm khi có khách “công thức dị” (điểm dừng, chờ đêm, bậc tấn).

**Quyết định:** không extract interface Phase 1. Khi cần: `IFreightRateLookup` (tên đúng việc: lookup bảng, chưa phải “engine”).

---

## 3. Rủi ro reviewer không nêu (vẫn liên quan)

| Rủi ro | Chỗ | Hướng |
|--------|-----|--------|
| Trùng số lệnh | `DocumentSequence` không khóa hàng | RowVersion + retry (P0) |
| Chứng từ mặc định local disk | `DocumentStorePath` rỗng | IFileStorage + seed/docs: go-live **bắt** share UNC (P0 docs + Settings cảnh báo) |
| Mật khẩu SQL trên client | `appsettings.json` | P1 vận hành: Windows auth / secret, không MediatR |
| `ICurrentUser` singleton | một process = một user | Đúng WPF; API Phase 2 phải scoped theo request |
| Application reference EF | `Include`/`ToListAsync` | Chấp nhận Phase 1; không thêm MediatR để “che” |

---

## 4. Kế hoạch sửa — theo đợt

Ưu tiên: **an toàn dữ liệu lệnh** trước abstraction Phase 2.

### Đợt 0 — Tài liệu (đợt này)

- File này: kết luận + kế hoạch.
- Khi đợt 1–2 merge: cập nhật [`Database.md`](Database.md) (cột `RowVersion`, `IsDeleted`, nghĩa `StoredPath`), [`Architecture.md`](Architecture.md) (file storage, concurrency). **Không** viết Temporal/MediatR như thể đã làm.

### Đợt 1 — P0 Concurrency — **đã làm**

`ROWVERSION` trên bốn bảng; `ConcurrencyConflict`; `ApplyOriginalRowVersion` khi save lệnh/bảng giá từ ViewModel; `NextAsync` retry 3 lần; `004_concurrency.sql`; EnsureCreated phát hiện thiếu `RowVersion`.

**Schema** [`database/001_schema.sql`](../database/001_schema.sql): thêm cột, không DROP DB nếu có thể — dùng script additive `database/004_concurrency.sql` (ALTER) **và** sửa `001` cho DB mới:

```sql
ALTER TABLE dbo.DispatchOrder ADD RowVersion ROWVERSION NOT NULL;
-- tương tự PriceList, FreightStatement, DocumentSequence
```

`ROWVERSION` SQL Server tự điền; không ETL.

**Domain:** property `byte[] RowVersion { get; set; } = [];` trên đúng 4 entity (không nhét vào `Entity` base — Company/ChangeLog không cần).

**EF** `HmaDbContext`: `.Property(x => x.RowVersion).IsRowVersion();`

**Application:**

- Helper `Concurrency.Rethrow(DbUpdateConcurrencyException)` → tiếng Việt (một file, một type).
- `DispatchOrderService` Save/Lock/SetStatus/Reconcile/Unreconcile: bắt concurrency.
- `PriceListService.SaveAsync` / khóa: bắt.
- `FreightStatementService.GenerateAsync`: bắt.
- `DocumentNumberService.NextAsync`: vòng lặp tối đa 3, bắt concurrency rồi `Find` lại sequence.

**Test:** `Hma.Application.Tests` — mock/in-memory khó cho rowversion SQL thật. Tối thiểu: unit test message helper; nếu có test DB sau này thì hai Save lệch token.

**UI:** không thêm dialog đặc biệt; toast đã có. Optional: `DispatchWorkspaceViewModel` khi message chứa “Tải lại” thì `OpenAsync(Editor.Id)` — làm nếu toast-only dễ bỏ sót.

**Dev:** `EnsureCreated` — EF tạo `RowVersion` nếu entity có. Clone cũ: chạy `004` hoặc để app drop khi thiếu cột (hiện chỉ check `Partner` / `SenderCustomerId`). **Mở rộng** `EnsureCreatedAndSeededAsync`: nếu thiếu cột `DispatchOrder.RowVersion` thì rebuild **hoặc** document “chạy 004”. Ưu tiên: phát hiện cột + `EnsureDeleted` giống schema cũ (dev LocalDB chấp nhận mất data).

### Đợt 2 — P0 IFileStorage — **đã làm**

**Mới (một type / file):**

- `Hma.Application/Abstractions/IFileStorage.cs`
- `Hma.Infrastructure.SqlServer/LocalDiskFileStorage.cs` (adapter; không tạo project mới)
- `Hma.Application/Services/FileStorageRoot.cs` **không** — root path resolve trong infrastructure từ `IHmaDbContext` parameters **hoặc** inject `IFileStorage` đã bind root lúc resolve.

Gọn: `LocalDiskFileStorage(IHmaDbContext db)` đọc `DocumentStorePath` mỗi lần Save (đổi settings không restart).

**Sửa:** `DispatchDocumentService.AttachAsync` / `DeleteAsync` chỉ gọi `IFileStorage`. Bỏ `File.*` khỏi Application.

**DI:** `AddHmaInfrastructure` đăng ký `IFileStorage`.

**WPF:** đọc file dialog thành stream, truyền stream + fileName vào `AttachAsync(orderId, kind, fileName, stream)`.

**Key format:** `{orderId:000000}/{yyyyMMddHHmmssfff}-{guid}-{safeFileName}` — an toàn path (bỏ `..`, `\`) và không ghi đè file trùng tên.

**Cắt:** file đã lưu path tuyệt đối trước khi đổi — Phase 1 chưa production; không viết migrator path. Ghi trong Database.md.

**Settings:** nếu `DocumentStorePath` rỗng, toast khi đính kèm: “Đang lưu máy local — đặt đường dẫn mạng trong Tham số trước khi dùng nhiều máy.” Không chặn lưu (dev một máy).

### Đợt 3 — P1 Soft-delete lệnh + lỗi FK — **đã làm**

- `DispatchOrder.IsDeleted BIT NOT NULL CONSTRAINT DF_DispatchOrder_IsDeleted DEFAULT (0)`
- EF `HasQueryFilter(o => !o.IsDeleted)` — **mọi** query lệnh ẩn đã xóa. `GetAsync` kể cả đã xóa: `.IgnoreQueryFilters()` chỉ cho màn admin (không làm Phase 1).
- `DeleteAsync`: set `IsDeleted = true`, không `Remove` (vẫn chặn Locked/Reconciled).
- `SearchAsync` / bảng kê / dashboard: filter tự có.
- `CatalogService.DeleteAsync` / `CustomerService.DeleteAsync`: try/catch `DbUpdateException` → câu Việt.

Không soft-delete Customer/Partner ở đợt này.

### Đợt 4 — Cố ý không làm (trừ khi đổi brief)

- MediatR, grid Command/Query, pipeline behaviors.
- Temporal Tables; xóa bảng `ChangeLog`.
- `ISoftDelete` + filter mọi bảng.
- `IPricingStrategy` / `StandardPricingStrategy`.
- MinIO / S3.
- Host ASP.NET, JWT, Swagger.

---

## 5. Lộ trình 3-tier (Phase 2) — làm đúng việc

Khi có chi nhánh / không mở SQL WAN:

```
WPF / future Web  --HTTP-->  Hma.Host.Api  -->  Application + IHmaDbContext  -->  SQL
                         IFileStorage (disk hoặc S3)
                         ICurrentUser scoped theo request (không singleton)
```

Checklist host:

- Connection string chỉ trên server.
- WPF bỏ `AddHmaInfrastructure`.
- Auth: session/cookie hoặc Windows — **không** copy JWT vào Application.
- `IFileStorage` đã có từ Đợt 2 — đổi implement, không đổi use case.

MediatR lúc đó **tùy chọn** (logging pipeline), không phải điều kiện lên cloud.

---

## 6. Thứ tự thi công và chấp nhận

```
Đợt 1 RowVersion  →  Đợt 2 IFileStorage  →  Đợt 3 IsDeleted lệnh + FK message
```

Độc lập tương đối: 2 không phụ thuộc 1; làm 1 trước vì mất dữ liệu cước nặng hơn path file.

**Xong Đợt 1 khi:** hai Save lệch `RowVersion` trên cùng `DispatchOrder` → một lỗi tiếng Việt; `NextAsync` không cấp trùng số dưới test đua (hoặc retry chứng minh).

**Xong Đợt 2 khi:** Application không còn `System.IO.File`; đính kèm ghi key tương đối; xóa chứng từ gọi storage.

**Xong Đợt 3 khi:** xóa lệnh chưa đối soát không DROP hàng (IsDeleted=1); search không thấy; xóa khách đang có lệnh → toast Việt.

---

## 7. File sẽ đụng (khi implement)

**Đợt 1:** `001_schema.sql`, `004_concurrency.sql` (mới), `DispatchOrder.cs`, `PriceList.cs`, `FreightStatement.cs`, `DocumentSequence.cs`, `HmaDbContext.cs`, `DispatchOrderService.cs`, `PriceListService.cs`, `FreightStatementService.cs`, `DocumentNumberService.cs`, `ConcurrencyConflict.cs` (mới), `Database.md`.

**Đợt 2:** `IFileStorage.cs`, `LocalDiskFileStorage.cs`, `Infrastructure/DependencyInjection.cs`, `DispatchDocumentService.cs`, `DispatchWorkspaceViewModel.cs` (stream), `Database.md`, `Architecture.md`.

**Đợt 3:** `001_schema` / `005_soft_delete.sql`, `DispatchOrder.cs`, `HmaDbContext` filter, `DispatchOrderService.DeleteAsync`, `CustomerService` / `CatalogService` catch FK, tests quyền xóa nếu có.

Không sửa `legacy/`. Không đụng cash/VAT đóng băng trừ khi RowVersion lan sang — **không lan**.
