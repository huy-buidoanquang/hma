# Legacy inventory — DHXE / Hà Minh Anh

Source of truth: `legacy/MainNC`, `legacy/DesignNC`, `legacy/Bin` reports, `legacy/Update T12015`, `legacy/DB` (SQL Server 2000 MDF, version 539). Modern LocalDB cannot attach or restore this database; schema below is reconstructed from VB handlers, form XML, and exported CRM procedures.

## Runtime
- Shell: `legacy/Bin/PCMNavigator.exe` + closed-source Plexis DLLs
- Config: `legacy/Bin/maui.config` → database `DHXE`, server `(local)`
- Business DLLs: `Nil.dll`, `DanhMuc.dll`
- DB files: `legacy/DB/DHXE_Data.MDF` (SQL 2000), `legacy/BK05052022/BK05052022.bak` (also version 539)

## Forms (Plexis + DesignNC)

| Form | Module | CRUD SPs / notes |
|------|--------|------------------|
| `frmKhachHang` / find | Customer | `ql_khachhang_load/save/find`, `ql_khachhang_cbo_load` |
| `frmGNDMNhanVien` / find | Employee | `ql_nhanvien_load/find`, `ql_nhanvien_cbo_load`, `ql_nhanvien_cbo_load_laixe` |
| `frmGNThanhPho` / find | City | `gn_thanhpho_find` |
| `frmGNDMPhongBan` | Department | `gn_phongban_load/find` |
| `frmGNDMChucVu` | Job title | `gn_chucvu_load`, `gn_ChucVu_find` |
| `frmTenBangGia` / find | Price list name | `ql_tenbg_load/find` |
| `frmBangGia` / find | Price list | `ql_banggia_find`, `ql_banggia_ct_load`, `ql_banggia_ct_load_cuoc` |
| `frmLenhDieuXe` / find | Dispatch (NIL) | `ql_nil_load/find/save`, `ql_nil_ct_load`, `ql_nil_load_id_max` |
| `frmPhieuThu` / find | Cash receipt | `ql_phieuthu_find/save`, `ql_phieuthu_load_id_max`, `ql_nil_cbo_load_pt/khpt` |
| `frmPhieuChi` / find | Cash payment | `ql_phieuchi_load/save/delete/find`, `ql_phieuchi_load_id_max` |
| HD GTGT / find / export | VAT invoice | `ql_hdgtgt_*`, `gn_hdgtgt_find`, `ql_hdgtgt_ep_find` |
| Tham số HT | Sequences | `gn_sinhma_load`, `gn_sinhma_load_all` |

## Stored procedures called from Nil/DanhMuc

**Business (`ql_*`):** `ql_khachhang_load`, `ql_khachhang_find`, `ql_khachhang_cbo_load`, `ql_nhanvien_load`, `ql_nhanvien_find`, `ql_nhanvien_cbo_load`, `ql_nhanvien_cbo_load_laixe`, `ql_tenbg_load`, `ql_tenbg_find`, `ql_banggia_find`, `ql_banggia_ct_load`, `ql_banggia_ct_load_cuoc`, `ql_nil_load`, `ql_nil_find`, `ql_nil_ct_load`, `ql_nil_load_id_max`, `ql_nil_cbo_load_pt`, `ql_nil_cbo_load_khpt`, `ql_phieuthu_find`, `ql_phieuthu_load_id_max`, `ql_phieuchi_load`, `ql_phieuchi_save`, `ql_phieuchi_delete`, `ql_phieuchi_find`, `ql_phieuchi_load_id_max`, `ql_phieuchi_load_bc`, `ql_hdgtgt_find`, `ql_hdgtgt_lib_load`, `ql_hdgtgt_lib_load_dich`, `ql_hdgtgt_ct_load_tien`, `ql_hdgtgt_ct_save`, `ql_hdgtgt_ct_delete`, `ql_hdgtgt_load_id_max`, `ql_hdgtgt_checkexists`, `ql_hdgtgt_ep_find`

**General (`gn_*`):** `gn_sinhma_load`, `gn_sinhma_load_all`, `gn_thanhpho_find`, `gn_phongban_load`, `gn_phongban_find`, `gn_chucvu_load`, `gn_ChucVu_find`, `gn_pq_load_formID_formname`, `gn_pq_load_quyen`, `gn_hdgtgt_find`, `gn_hdgtgt_load_ctvd`

**Auth (`crm_*`) — exported from MDF:** `crm_check_password`, `crm_user_load/save/find/delete`, `crm_user_form_*`, `crm_get_permission`, `crm_nhanvien_load`, `crm_congty_load`, `crm_treeview_phanquyen_load`

**Function:** `TANG_MA_SO_PC` (phiếu chi numbering via `sinhma.sopc_ud`)

## Inferred business tables

| Table | Key columns |
|-------|-------------|
| `khachhang` | `kh_id`, `kh_ud`, `kh_nm`, `diachi`, `sodt`, `masothue`, `thanhpho_id`, `loai_kh`, `ngaycapnhat` |
| `nhanvien` | `nhanvien_id`, `nhanvien_ud`, `nhanvien_nm`, `diachi`, `sodt`, `didong`, `ngaysinh`, `socmnd`, `phongban_id`, `chucvu_id`, `biensoxe`, `ngaycapnhat` |
| `thanhpho` | `thanhpho_id`, `thanhpho_ud`, `thanhpho_nm`, `mota` |
| `phongban` | `phongban_id`, `phongban_ud`, `phongban_nm` |
| `chucvu` | `chucvu_id`, `chucvu_ud`, `chucvu_nm` |
| `tenbg` | `tenbg_id`, `tenbg_ud`, `tenbg_nm`, `ngaylap`, `ngaykhoa`, `lydokhoa`, locked flag |
| `banggia` | `bg_id`, `tenbg_id`, `nhanvien_id`, `ngaylap` |
| `banggia_ct` | `thanhpho_id`, `tenbg_id`, `cuocxe125/35/5/145/8`, `id125/35/5/145/8` |
| `nil` | `nil_id`, `nil_ud`, `ngaytao`, `nguoigui_id`, `nguoinhan_id`, `loai_khg`, `loai_khn`, `loaixe_id`, `bienso`/`laixe`, `hinhthuc_tt_id`, `nhanvien_id`, `cuoc`, `thukhac`, `tongthu`, `is_chitien` |
| `nil_ct` | cargo lines: `tenhang`, `sokien`, `hanhtrinh`, `sokm`, `ghichu` |
| `phieuthu` | `sopt_id`, `sopt_ud`, `ngaytao`/`ngaycapnhat`, `loaipt` (0=NIL, 1=KH), `doituong_id`, `kh_id`, `sotien`/`tongtien`, `doctien`, `lydonoptien`, `nhanvien_id` |
| `phieuchi` | `sopc_id`, `sopc_ud`, `ngaytao`, `loaipc` (1=KH, 2=lái xe), `laixe_id`, `kh_id`, `sotien`, `doctien`, `hoten`, `diachi`, `lydonoptien`, `nhanvien_id` |
| `hdgtgt` | `hdgtgt_id`, `kh_id`, `phanloai`, VAT/qty/total, `nhanvien_id` |
| `hdgtgt_ct` | `hdgtgt_ct_id`, `hdgtgt_id`, `nil_id` |
| `sinhma` | `sinhma_ud`, `giatri` (counters + `VAT`, `User`, `Pass`) |
| `user` | `username_id`, `username`, `password`, `quyenquanly`, `nhanvien_id`, `is_dacbiet`, `menu_show` |
| `user_form` | `user_id`, `form_id`, `them`, `xoa`, `sua`, `xem`, `inan` |
| `congty` | company header for invoices |
| Plexis meta | `ui_engine_form`, `ui_engine_control`, `ui_navigation_node`, `business_object` — **not migrated** |

## Reports in `legacy/Bin`

| File | Purpose |
|------|---------|
| `rpt_Lenhdieuxe.rpt` | Print dispatch order |
| `rpt_phieuthu_ldx.rpt` | Print cash receipt |
| `rpt_phieuchi_ldx.rpt` | Print cash payment |
| `rpt_hdgtgt_ldx.rpt` / `_ko` | VAT invoice |
| `rpt_ktnc02_bcngay_lenhdx.rpt` | Daily dispatch report |
| `rpt_ktnc02_ldx_thkh.rpt` | Dispatch by customer |
| `rpt_ktnc04_bcngay_lenhdx*.rpt` | Daily dispatch + franchise variants (chưa nhượng / cố định / được nhượng) |
| `rpt_ktnc05_bcngay_phieuchi.rpt` | Daily cash payment |
| `FileMauHDGTGTRA.xls` | VAT Excel export template (0/5/10%) |

## Permissions
Per navigation node: 1 Thêm, 2 Xóa, 3 Sửa, 4 Xem, 5 In ấn. Manager (`quyenquanly`) bypasses. Special user flag `is_dacbiet`.
