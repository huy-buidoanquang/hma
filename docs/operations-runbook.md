# HMA Operations Runbook

## Môi trường và bí mật

- Cài .NET 10 Desktop Runtime và SQL Server 2016+; máy phát triển dùng LocalDB.
- Cấp chuỗi kết nối bằng `HMA_CONNECTION` hoặc `ConnectionStrings__Hma`. Không lưu mật khẩu SQL trong repository.
- Database trống cần `HMA_BOOTSTRAP_ADMIN_PASSWORD` đủ mạnh cho lần khởi tạo đầu tiên. Xóa biến này sau khi đăng nhập thành công.
- Với nhiều máy trạm, đặt `DocumentStorePath` là UNC share đã giới hạn quyền. Không vận hành production bằng fallback `%LocalAppData%`.

## Triển khai và kiểm tra trước go-live

1. Backup database và kho chứng từ hiện tại.
2. Chạy `dotnet tool restore`, `dotnet build src/Hma.slnx`, `dotnet test src/Hma.slnx` và kiểm tra migration drift bằng `dotnet ef migrations has-pending-model-changes --project src/Hma.Infrastructure.SqlServer --startup-project src/Hma.Infrastructure.SqlServer`.
3. Áp migration bằng chính executable hoặc `dotnet ef database update` trong cửa sổ bảo trì. Không sửa schema trực tiếp.
4. Mở **Cấu hình → Kiểm tra hệ thống**. Yêu cầu SQL và kho chứng từ đều “tốt”; production nhiều máy phải báo đang dùng thư mục dùng chung.
5. Kiểm tra quyền của một tài khoản điều phối, một kế toán và một quản lý; xác nhận maker–checker không cho người gửi tự duyệt.

## Cutover dữ liệu

- Chạy ETL theo thứ tự trong `docs/etl-cutover.md` trên bản sao database legacy, không chạy trực tiếp lần đầu trên production.
- Thực hiện ít nhất hai rehearsal độc lập. Mỗi lần phải chạy `database/etl/99_validate.sql` và lưu kết quả count, tổng tiền, orphan, duplicate, kỳ kế toán và user.
- Chỉ cutover khi hai lần đối chiếu giống nhau và danh sách sai lệch đã được ký xác nhận nghiệp vụ.

## Backup

- SQL Server: full backup hằng đêm, differential mỗi 6 giờ, transaction log mỗi 15 phút nếu dùng Full recovery.
- Kho chứng từ: snapshot/copy theo cùng mốc full backup SQL. Giữ mapping `DispatchDocument.StoredPath` tương đối.
- Mã hóa bản backup, tách quyền ghi của tài khoản ứng dụng khỏi nơi lưu bản sao, và giữ ít nhất một bản offline/immutable.
- Ghi lại thời điểm, checksum, người thực hiện và chính sách retention.

## Restore rehearsal

Mỗi tháng, restore vào database và thư mục tạm biệt lập; không ghi đè production. Chạy migration, đăng nhập bằng tài khoản kiểm thử, mở ngẫu nhiên chứng từ, đối chiếu bảng kê/quyết toán và chạy health check. Ghi nhận RPO/RTO thực tế và xóa môi trường thử sau nghiệm thu.

## Sự cố và rollback

- Nếu migration thất bại, dừng client mới, giữ nguyên log lỗi và phục hồi full/differential/log backup đã kiểm chứng; không tự viết `ALTER` chữa nóng.
- Nếu kho chứng từ lỗi, ngừng upload/xóa file nhưng giữ SQL online để tra cứu; khôi phục share rồi chạy health check.
- Không sửa trực tiếp cước đã đối soát. Dùng quy trình từ chối/hủy hoặc chứng từ điều chỉnh có log nghiệp vụ.
