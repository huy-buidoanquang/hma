# 🚚 BRIEF YÊU CẦU PHẦN MỀM QUẢN LÝ LỆNH ĐIỀU XE & ĐỐI SOÁT CƯỚC VẬN CHUYỂN

---

## 🌟 TỔNG QUAN DỰ ÁN

### Mục tiêu
Xây dựng phần mềm hỗ trợ bộ phận Điều phối quản lý tập trung toàn bộ hoạt động điều xe, từ việc tiếp nhận yêu cầu vận chuyển, tạo và phát hành lệnh điều xe, phân công tài xế/phương tiện, theo dõi tình trạng chuyến hàng đến đối soát và tổng hợp cước vận chuyển theo từng khách hàng, đối tác.

Phần mềm giúp thay thế việc quản lý bằng Excel, giấy tờ hoặc các file rời rạc; đảm bảo dữ liệu được cập nhật tập trung, dễ tra cứu, hạn chế sai sót và tạo nền tảng để bộ phận Kế toán lập bảng kê cước vận chuyển hàng tháng.

---

## 🏢 1. QUẢN LÝ KHÁCH HÀNG
Lưu trữ toàn bộ thông tin khách hàng sử dụng dịch vụ vận chuyển.

**Thông tin khách hàng bao gồm:**
*   **Mã khách hàng**
*   **Tên khách hàng / Tên công ty**
*   **Mã số thuế**
*   **Địa chỉ**
*   **Người liên hệ**
*   **Số điện thoại**
*   **Email**
*   **Kế toán phụ trách khách hàng**

---

## 🤝 2. QUẢN LÝ ĐỐI TÁC
Lưu trữ toàn bộ thông tin đối tác cung cấp xe.

**Thông tin đối tác bao gồm:**
*   Mã đối tác, Tên đối tác / Tên công ty
*   Mã số thuế, Địa chỉ
*   Người liên hệ, Số điện thoại, Email

### Thiết lập bảng giá
Cho phép thiết lập cước vận chuyển theo bảng giá chung hoặc áp dụng từng khách hàng:
*   Khách hàng
*   Tuyến vận chuyển (Điểm lấy hàng ➔ Điểm trả hàng)
*   Loại xe & Tải trọng
*   Đơn giá & Phụ phí
> 💡 *Hệ thống có thể tự động lấy mức cước đã thiết lập khi tạo lệnh điều xe.*

---

## 🚛 3. QUẢN LÝ XE, TÀI XẾ
Quản lý tập trung hồ sơ và lịch sử hoạt động của từng xe, tài xế *(HMA có nhiều công ty đối tác, các xe thuộc các công ty đối tác)*.

**Thông tin tài xế & phương tiện:**
*   Mã tài xế, Họ tên, Số điện thoại, Ngày sinh, Số CCCD
*   Biển kiểm soát xe, Trọng tải
*   Thuộc công ty đối tác nào?

---

## 📝 4. TẠO LỆNH ĐIỀU XE
*Đây là chức năng trung tâm của hệ thống.*

**Thông tin trên lệnh điều xe:**
*   **Thông tin chung:** Số lệnh điều xe, Ngày tạo lệnh, Khách hàng, Người tạo lệnh, Trạng thái lệnh.
*   **Thông tin chuyến:** Ngày giờ lấy hàng, Địa điểm lấy hàng, Địa điểm giao hàng.
*   **Thông tin phương tiện:** Biển số xe, Tải trọng.
*   **Thông tin tài xế:** Tên tài xế.
*   **Thông tin cước:** Đơn giá vận chuyển, Phụ phí, Chi phí phát sinh, Tổng cước, Ghi chú.

---

## 📎 5. QUẢN LÝ CHỨNG TỪ
Mỗi lệnh điều xe cần có khu vực lưu trữ chứng từ liên quan.
*   Cho phép upload: **Lệnh điều xe, Biên bản giao hàng, Hóa đơn/chứng từ liên quan, Các tài liệu khác.**
*   Mỗi chứng từ được gắn trực tiếp với mã lệnh/mã chuyến, giúp tra cứu nhanh khi cần đối soát.

---

## 💰 6. QUẢN LÝ CƯỚC VẬN CHUYỂN
Mỗi chuyến hoàn thành sẽ ghi nhận:
*   **Khách hàng:** Khách hàng sử dụng dịch vụ
*   **Mã lệnh:** Mã lệnh điều xe
*   **Ngày chạy:** Ngày thực hiện
*   **Tuyến:** Điểm lấy ➔ Điểm giao
*   **Phương tiện & Người thực hiện:** Biển số xe, Tài xế
*   **Chi phí:** Đơn giá (Cước vận chuyển), Phụ phí (nếu có), Phát sinh (Chi phí phát sinh), Tổng cước (Giá trị cuối cùng)
*   **Trạng thái:** Chưa đối soát / Đã đối soát

---

## ✅ 7. ĐỐI SOÁT CHUYẾN
Trước khi đưa vào bảng kê tháng, hệ thống cần có bước đối soát.
**Kế toán kiểm tra:**
- [ ] Đã có chứng từ giao hàng chưa?
- [ ] Đúng tuyến chưa?
- [ ] Đúng đơn giá chưa?
- [ ] Có phụ phí không?
- [ ] Có phát sinh không?
- [ ] Số tiền cuối cùng là bao nhiêu?

**Quy trình xác nhận:** `Chưa đối soát` ➔ `Đã đối soát`.
> ⚠️ *Những chuyến chưa hoàn thiện chứng từ hoặc chưa xác nhận cước sẽ không được đưa tự động vào bảng kê chính thức.*

---

## 📊 8. LẬP BẢNG KÊ CƯỚC THEO THÁNG
*Đây là chức năng đầu ra quan trọng nhất.*
Người dùng chọn: **[Khách hàng] + [Tháng/Năm]** ➔ Hệ thống tự động tổng hợp toàn bộ chuyến đã hoàn thành và đã đối soát trong kỳ.

**Ví dụ: BẢNG KÊ CƯỚC VẬN CHUYỂN THÁNG 08/2026 - Khách hàng: ABC**

| STT | Ngày | Mã lệnh | Tuyến | Biển số | Trọng tải | Tài xế | Cước | Phụ phí | Tổng tiền | Ghi chú |
|:---:|:---|:---|:---|:---|:---|:---|---:|---:|---:|:---|
| 1 | 01/08 | LX001 | HN ➔ HP | 29A-xxx | 1,25 tấn | Nguyễn A | 3.000.000 | 0 | 3.000.000 | |
| 2 | 03/08 | LX002 | HN ➔ BN | 29C-xxx | 3,5 tấn | Nguyễn B | 2.500.000 | 200.000 | 2.700.000 | |
| 3 | 05/08 | LX003 | HN ➔ HP | 29A-xxx | 10 tấn | Nguyễn A | 3.000.000 | 0 | 3.000.000 | |
| **Tổng**| | | | | | | | | **8.700.000** | |

**Hệ thống tự động tính:** Tổng số chuyến, Tổng cước, Tổng phụ phí, Tổng phát sinh, Tổng tiền phải thanh toán, Tổng tiền sau VAT.

---

## 📥 9. XUẤT BẢNG KÊ
*   **Xuất Excel:** Phục vụ Kế toán kiểm tra, đối soát nội bộ, gửi khách hàng, lưu trữ.
*   **Xuất PDF:** Phục vụ in bảng kê, gửi khách hàng, lưu hồ sơ.
*(Có thể thiết kế mẫu bảng kê theo format riêng của từng khách hàng nếu cần).*

---

## 📈 10. BÁO CÁO QUẢN TRỊ
Hệ thống cần có báo cáo để Ban Giám đốc theo dõi hoạt động vận tải.
*   **Báo cáo theo khách hàng:** Số chuyến, Doanh thu/cước, Sản lượng vận chuyển, Cước theo tháng.
*   **Báo cáo theo xe:** Số chuyến/xe, Doanh thu/xe, Tuyến đã chạy.
*   **Báo cáo theo thời gian:** Theo Ngày / Tuần / Tháng / Quý / Năm.

---

## 🔍 11. TÌM KIẾM & TRA CỨU
Cho phép tìm kiếm nhanh theo: `Mã lệnh`, `Khách hàng`, `Biển số xe`, `Tuyến`, `Ngày chạy`, `Trạng thái`, `Tháng`.
> 🎯 **Mục tiêu:** Chỉ cần nhập mã lệnh hoặc biển số xe là có thể truy xuất toàn bộ lịch sử chuyến.

---

## 🔐 12. PHÂN QUYỀN NGƯỜI DÙNG

**Kế toán:**
*   Lập, xem, sửa lệnh.
*   Kiểm tra cước, đối soát.
*   Quản lý & Xuất bảng kê.
*   Theo dõi công nợ/cước.

**Quản lý/Giám đốc:**
*   Phân quyền cho từng người.
*   Xem dashboard, báo cáo.
*   Xem toàn bộ dữ liệu.
*   Khóa lệnh.

---

## 🕒 13. LỊCH SỬ THAY ĐỔI DỮ LIỆU
Hệ thống cần lưu lại lịch sử:
*   Ai tạo lệnh? Ai sửa lệnh?
*   Sửa nội dung gì? Thời gian sửa?
*   Ai thay đổi cước? Ai đối soát?
*(Đặc biệt với đơn giá và tổng cước, cần kiểm soát lịch sử thay đổi để hạn chế sai lệch dữ liệu).*

---

## 🖥️ 14. CÁC CHỈ SỐ CẦN HIỂN THỊ TRÊN DASHBOARD
**Cước vận chuyển:**
*   Tổng cước tháng
*   Chưa đối soát
*   Đã đối soát
*   Chờ chứng từ

---

## 🚀 MỤC TIÊU SAU KHI TRIỂN KHAI
Phần mềm cần giải quyết 5 vấn đề chính:
1.  **Quản lý tập trung:** Toàn bộ lệnh điều xe, tài xế, phương tiện, chuyến hàng và cước được quản lý trên một hệ thống.
2.  **Giảm sai sót:** Hạn chế nhập liệu trùng, thất lạc lệnh, nhầm xe, nhầm tài xế, nhầm tuyến hoặc sai cước.
3.  **Đối soát nhanh:** Dữ liệu chuyến hàng và cước được liên kết trực tiếp với nhau, giảm thời gian tổng hợp thủ công.
4.  **Tự động hóa bảng kê tháng:** Từ dữ liệu các lệnh điều xe đã hoàn thành, hệ thống tự động tổng hợp thành bảng kê cước theo từng khách hàng, từng tháng.

---

## 🎯 KẾT QUẢ KỲ VỌNG
Phần mềm sau khi hoàn thiện phải tạo thành một chuỗi dữ liệu khép kín:

> **KHÁCH HÀNG ➔ LỆNH ĐIỀU XE ➔ XE + TÀI XẾ ➔ CHUYẾN HÀNG ➔ CHỨNG TỪ ➔ CƯỚC ➔ ĐỐI SOÁT ➔ BẢNG KÊ THÁNG**

Điểm quan trọng nhất là **không nhập lại dữ liệu nhiều lần**, có thể chỉnh sửa lệnh dễ dàng. Một lệnh điều xe được tạo từ đầu phải trở thành dữ liệu gốc để hệ thống sử dụng xuyên suốt cho việc theo dõi chuyến, quản lý cước và cuối cùng tự động tạo bảng kê cho khách hàng.
