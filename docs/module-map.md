# Module map — find / detail (legacy UX + brief)

Permission model: `AppScreen.Key` + Create / Delete / Update / View / Print (`Return_PQ` 1–5). Managers (`IsManager`) bypass. Accountant users get per-screen flags. Buttons Lưu / Xóa / In / Khóa are disabled from the same flags (nav hide is not enough).

Pattern: **Tìm → lưới → Xem / double-click → form chi tiết**. Catalogs stay two-column (find left, editor right). Dispatch is list on top, form below.

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
| `cities` | CityView | Thành phố lấy/giao |
| `price-lists` | PriceListView | Hiệu lực từ–đến; khóa + lý do; sửa/xóa dòng giá; tuyến × loại xe |
| `dispatch-orders` | DispatchView | Find: số LDX, từ–đến, KH, biển, loại xe, TP giao, TT lệnh, TT đối soát, khoảng tiền. Chi tiết: người gửi/nhận (VL-), giờ lấy, người tạo, lưới hàng, auto cước, in 1 lệnh / ngày / tháng / theo khách, Excel danh sách cước |
| `lookup` | LookupView | Ô số lệnh hoặc biển số → lịch sử chuyến, mở lệnh |
| `reconcile` | ReconcileView | Checklist; mở lệnh từ lưới; Pending → Reconciled; audit ChangeLog |
| `statements` | StatementView | Bảng kê tháng; cột tải/phát sinh/ghi chú; tổng cước / phụ phí / phát sinh / trước–sau VAT |
| `reports` | ReportView | In ngày; preset tuần / quý / năm; số chuyến + tổng cước theo khách/xe |
| `settings` | SettingsView | VatRate, DocumentStorePath, số đếm `dispatch-order` / `freight-statement` |
| `users` | UserView | Manager-only |

Frozen (no nav): `cash-receipts`, `cash-payments`, `vat-invoices`.

## Roles
- **Kế toán:** lệnh, đối soát, bảng kê, danh mục, tra cứu, dashboard/báo cáo (view/in).
- **Quản lý:** all of the above + users + settings + khóa lệnh / khóa bảng giá.

## ETL
- `database/etl/03_dispatch.sql` maps `nguoigui_id` → `SenderCustomerId` + snapshot, `nguoinhan_id` → `ReceiverCustomerId` + snapshot; `CustomerId` = người gửi (bill-to). Cargo from `nil_ct`.
- No ETL 04/05 in this phase.
