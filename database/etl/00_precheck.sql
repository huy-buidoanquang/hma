-- Row counts on legacy DHXE. Run against the SQL 2000 source before ETL.
-- sqlcmd -S $(LegacyServer) -d DHXE -i 00_precheck.sql -o precheck.txt

SET NOCOUNT ON;
SELECT 'khachhang' AS TableName, COUNT(*) AS RowCount FROM khachhang
UNION ALL SELECT 'nhanvien', COUNT(*) FROM nhanvien
UNION ALL SELECT 'thanhpho', COUNT(*) FROM thanhpho
UNION ALL SELECT 'phongban', COUNT(*) FROM phongban
UNION ALL SELECT 'chucvu', COUNT(*) FROM chucvu
UNION ALL SELECT 'tenbg', COUNT(*) FROM tenbg
UNION ALL SELECT 'banggia', COUNT(*) FROM banggia
UNION ALL SELECT 'banggia_ct', COUNT(*) FROM banggia_ct
UNION ALL SELECT 'hinhthuc_tt', COUNT(*) FROM hinhthuc_tt
UNION ALL SELECT 'nil', COUNT(*) FROM nil
UNION ALL SELECT 'nil_ct', COUNT(*) FROM nil_ct
UNION ALL SELECT 'phieuthu', COUNT(*) FROM phieuthu
UNION ALL SELECT 'phieuchi', COUNT(*) FROM phieuchi
UNION ALL SELECT 'hdgtgt', COUNT(*) FROM hdgtgt
UNION ALL SELECT 'hdgtgt_ct', COUNT(*) FROM hdgtgt_ct
UNION ALL SELECT '[user]', COUNT(*) FROM [user];

SELECT SUM(CAST(tongthu AS DECIMAL(20,2))) AS DispatchTotal FROM nil;
SELECT MAX(nil_ud) AS LastDispatchCode FROM nil;
SELECT hinhthuc_tt_id, hinhthuc_tt_ud, hinhthuc_tt_nm FROM hinhthuc_tt ORDER BY hinhthuc_tt_id;
