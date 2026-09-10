# Module map — find / detail (legacy UX + brief)

Permission model: `AppScreen.Key` + Create / Delete / Update / View / Print (`Return_PQ` 1–5). Managers (`IsManager`) bypass. Accountant users get per-screen flags. Buttons Lưu / Xóa / In / Khóa are disabled from the same flags (nav hide is not enough).

Pattern: **Tìm → lưới → Xem / double-click → form chi tiết**. Catalogs stay two-column (find left, editor right). Dispatch form mode is list on top, form below.

**Main nav** is not 1:1 with `AppScreen.Key`. Shell items: Dashboard … Chức vụ, **Tuyến đường** (hub, workspace key `route-catalog` — not an AppScreen), Bảng giá, **Lệnh điều xe** (hub), Tra cứu, Đối soát, Bảng kê, Báo cáo, **Cấu hình** (hub). Thành phố / Người dùng / Điểm / Tuyến / Sửa lệnh theo khách are inner sections; permission keys stay as below.

| Screen key | View | Use cases |
|------------|------|-----------|
| `dashboard` | DashboardView | 4 KPI tháng: tổng cước, chưa/đã đối soát, chờ chứng từ; lưới khách/xe |
| `customers` | CustomerView | Find mã/tên/địa chỉ/MST/ngày; Excel danh sách; form liên hệ, email, Kế toán |
| `partners` | PartnerView | Find mã/tên; form đối tác |
| `drivers` | DriverView | Find; ngày sinh; **lịch sử chuyến** theo tài xế |
| `vehicles` | VehicleView | Find biển số; **lịch sử chuyến** theo xe |
| `employees` | EmployeeView | Nhân viên; combo phòng ban / chức vụ |
| `departments` | DepartmentView | Danh mục phòng ban (nhập cho combo NV) |
| `job-titles` | JobTitleView | Danh mục chức vụ |
| `cities` | CityView (section Cấu hình) | Thành phố (địa chỉ khách). Nav Cấu hình nếu manager hoặc `Can(cities)` hoặc `Can(settings)` |
| `locations` | LocationView (section Tuyến đường) | Điểm lấy/giao/qua |
| `routes` | RouteView (section Tuyến đường) | Tuyến = dãy điểm có thứ tự (≥2). Hub **Tuyến đường** nếu manager hoặc `Can(locations)` / `Can(routes)` |
| `price-lists` | PriceListView | Hiệu lực từ–đến; biến động giá; khóa + lý do; dòng giá tuyến × loại xe |
| `dispatch-orders` | DispatchHubView → DispatchView | Find: số LDX, từ–đến, KH, biển, loại xe, điểm đi/đến (first/last stop), TT lệnh, TT đối soát, khoảng tiền. Chi tiết: người gửi/nhận (VL-), giờ lấy, người tạo, combo tuyến, auto cước, in 1 lệnh / ngày / tháng / theo khách, Excel danh sách cước. Nhập Excel: 6 tab theo sheet tải (1.25…10), sửa lưới, Check in-app, nhập subset đã chọn (`Create`) |
| `dispatch-grid-edit` | DispatchGridEditView (section lệnh) | Sửa nhiều lệnh theo khách: combo tuyến, xe; không tra cước. Không đứng riêng trên main nav |
| `lookup` | LookupView | Ô số lệnh hoặc biển số → lịch sử chuyến, mở lệnh (hub form + `OpenByIdAsync`) |
| `reconcile` | ReconcileView | Checklist; mở lệnh từ lưới; Pending → Reconciled; audit ChangeLog |
| `statements` | StatementView | Bảng kê tháng; cột tải/phát sinh/ghi chú; tổng cước / phụ phí / phát sinh / trước–sau VAT |
| `reports` | ReportView | In ngày; preset tuần / quý / năm; số chuyến + tổng cước theo khách/xe |
| `settings` | SettingsView | Hub Cấu hình: VAT / chứng từ / số đếm; công ty; thành phố; người dùng (manager); từ điển điểm (`LocationAlias`); từ điển tuyến (`RouteAlias`); từ điển khách (`CustomerAlias`) |
| `users` | UserView (section Cấu hình) | Manager-only |

Frozen (no nav): `cash-receipts`, `cash-payments`, `vat-invoices`.

## Roles
- **Kế toán:** lệnh, đối soát, bảng kê, danh mục, tra cứu, dashboard/báo cáo (view/in).
- **Quản lý:** all of the above + users + settings + khóa lệnh / khóa bảng giá.

## ETL
- `database/etl/03_dispatch.sql` maps `nguoigui_id` → `SenderCustomerId` + snapshot, `nguoinhan_id` → `ReceiverCustomerId` + snapshot; `CustomerId` = người gửi (bill-to). Cargo from `nil_ct`.
- No ETL 04/05 in this phase.
