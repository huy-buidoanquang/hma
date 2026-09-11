USE Hma;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

-- Never import plaintext legacy passwords. Imported accounts stay disabled until a manager
-- assigns a strong password in the HMA user-management screen.
SET IDENTITY_INSERT dbo.AppUser ON;
INSERT INTO dbo.AppUser (Id, UserName, PasswordHash, DisplayName, EmployeeId, IsManager, IsSpecial, CreatedAt, LegacyId)
SELECT
    username_id,
    username,
    N'DISABLED',
    mota,
    nhanvien_id,
    ISNULL(quyenquanly, 0),
    ISNULL(is_dacbiet, 0),
    ngaytao,
    username_id
FROM LEGACY.DHXE.dbo.[user];
SET IDENTITY_INSERT dbo.AppUser OFF;

INSERT INTO dbo.UserPermission (AppUserId, AppScreenId, CanCreate, CanDelete, CanUpdate, CanView, CanPrint)
SELECT
    uf.[user_id],
    s.Id,
    ISNULL(uf.them, 0),
    ISNULL(uf.xoa, 0),
    ISNULL(uf.sua, 0),
    ISNULL(uf.xem, 0),
    ISNULL(uf.inan, 0)
FROM LEGACY.DHXE.dbo.user_form uf
INNER JOIN LEGACY.DHXE.dbo.ui_navigation_node n ON n.ui_navigation_node_id = uf.form_id
INNER JOIN dbo.AppScreen s ON s.[Key] = CASE
    WHEN n.launch_form LIKE N'%KhachHang%' THEN N'customers'
    WHEN n.launch_form LIKE N'%NhanVien%' THEN N'employees'
    WHEN n.launch_form LIKE N'%ThanhPho%' THEN N'cities'
    WHEN n.launch_form LIKE N'%PhongBan%' THEN N'departments'
    WHEN n.launch_form LIKE N'%ChucVu%' THEN N'job-titles'
    WHEN n.launch_form LIKE N'%BangGia%' OR n.launch_form LIKE N'%TenBangGia%' THEN N'price-lists'
    WHEN n.launch_form LIKE N'%LenhDieuXe%' OR n.launch_form LIKE N'%Nil%' THEN N'dispatch-orders'
    WHEN n.launch_form LIKE N'%PhieuThu%' THEN N'cash-receipts'
    WHEN n.launch_form LIKE N'%PhieuChi%' THEN N'cash-payments'
    WHEN n.launch_form LIKE N'%HDGTGT%' THEN N'vat-invoices'
    ELSE N'settings'
END;

COMMIT;
GO
