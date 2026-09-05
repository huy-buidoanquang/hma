using Hma.Application.Services;
using Hma.Domain.Entities;
using Hma.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Hma.Infrastructure.SqlServer;

/// <summary>
/// Bộ dữ liệu diễn tập cho DB trống (không chạy khi đã có khách — tránh đè ETL).
/// Kịch bản: điều xe Bắc Bộ, tháng trước đã chốt bảng kê, tháng này còn việc đối soát.
/// </summary>
public static class DemoDataSeeder
{
    public static async Task SeedIfEmptyAsync(HmaDbContext db, CancellationToken ct = default)
    {
        if (await db.Customers.AnyAsync(ct))
            return;

        var today = DateTime.Today;
        var thisMonth = new DateTime(today.Year, today.Month, 1);
        var lastMonth = thisMonth.AddMonths(-1);
        var lastMonthEnd = thisMonth.AddDays(-1);
        var daysThis = DateTime.DaysInMonth(thisMonth.Year, thisMonth.Month);
        var past = Math.Max(1, today.Day - 1);

        var hn = City(db, "HN", "Hà Nội", "Nội thành / KCN Nội Bài, Thạch Thất, Hà Đông");
        var hp = City(db, "HP", "Hải Phòng", "Cảng Hải Phòng, KCN Đình Vũ, An Dương");
        var bn = City(db, "BN", "Bắc Ninh", "KCN Quế Võ, Yên Phong, Từ Sơn");
        var hd = City(db, "HD", "Hải Dương", "KCN Nam Sách, Cộng Hòa, Tân Trường");
        var hy = City(db, "HY", "Hưng Yên", "KCN Phố Nối, Như Quỳnh, Văn Lâm");
        var qn = City(db, "QN", "Quảng Ninh", "Hạ Long, Cái Lân, Uông Bí");
        var tn = City(db, "TN", "Thái Nguyên", "Sông Công, Phổ Yên, KCN Yên Bình");
        var vp = City(db, "VP", "Vĩnh Phúc", "KCN Khai Quang, Bình Xuyên");
        var nd = City(db, "ND", "Nam Định", "TP Nam Định, Ý Yên");
        var nb = City(db, "NB", "Ninh Bình", "TP Ninh Bình, Tam Điệp");
        await db.SaveChangesAsync(ct);

        Location Loc(City city)
        {
            var row = new Location { Code = city.Code, Name = city.Name, Description = city.Description, CityId = city.Id };
            db.Locations.Add(row);
            return row;
        }

        var locByCityId = new Dictionary<int, Location>
        {
            [hn.Id] = Loc(hn),
            [hp.Id] = Loc(hp),
            [bn.Id] = Loc(bn),
            [hd.Id] = Loc(hd),
            [hy.Id] = Loc(hy),
            [qn.Id] = Loc(qn),
            [tn.Id] = Loc(tn),
            [vp.Id] = Loc(vp),
            [nd.Id] = Loc(nd),
            [nb.Id] = Loc(nb)
        };
        await db.SaveChangesAsync(ct);

        var routeCache = new Dictionary<string, Route>();
        Route Pair(City from, City to)
        {
            var origin = locByCityId[from.Id];
            var dest = locByCityId[to.Id];
            var fingerprint = $"{origin.Id}-{dest.Id}";
            if (routeCache.TryGetValue(fingerprint, out var existing))
                return existing;
            var suffix = routeCache.Count + 1;
            var route = new Route
            {
                Code = $"{from.Code}-{to.Code}-{suffix}",
                Name = $"{from.Name} → {to.Name}",
                Fingerprint = fingerprint
            };
            db.Routes.Add(route);
            db.SaveChanges();
            db.RouteStops.Add(new RouteStop { RouteId = route.Id, Sequence = 0, LocationId = origin.Id });
            db.RouteStops.Add(new RouteStop { RouteId = route.Id, Sequence = 1, LocationId = dest.Id });
            db.SaveChanges();
            routeCache[fingerprint] = route;
            return route;
        }

        var dieuPhoi = Dept(db, "DP", "Điều phối");
        var keToan = Dept(db, "KT", "Kế toán");
        var banGd = Dept(db, "BGĐ", "Ban giám đốc");
        var nv = Title(db, "NV", "Nhân viên");
        var ktv = Title(db, "KTV", "Kế toán viên");
        var tp = Title(db, "TP", "Trưởng phòng");
        var gd = Title(db, "GD", "Giám đốc");
        await db.SaveChangesAsync(ct);

        var minh = Emp(db, "NV001", "Trần Văn Minh", "0904 512 378", dieuPhoi, tp,
            "Tổ 4, phường Bồ Đề, Long Biên, Hà Nội", new DateTime(1988, 3, 12), "001088003412");
        var hoa = Emp(db, "NV002", "Nguyễn Thị Hoa", "0912 886 441", keToan, ktv,
            "Ngõ 128, đường Giải Phóng, Hoàng Mai, Hà Nội", new DateTime(1992, 7, 21), "001192007821");
        var lan = Emp(db, "NV003", "Lê Thị Lan", "0983 227 190", keToan, nv,
            "KĐT Việt Hưng, Long Biên, Hà Nội", new DateTime(1995, 11, 5), "001195011105");
        var huy = Emp(db, "NV004", "Phạm Quốc Huy", "0903 218 009", banGd, gd,
            "Phố Nguyễn Khoái, Hai Bà Trưng, Hà Nội", new DateTime(1979, 1, 18), "001079001118");
        await db.SaveChangesAsync(ct);

        var admin = await db.Users.FirstAsync(u => u.UserName == "admin", ct);
        var ketoan = await db.Users.FirstAsync(u => u.UserName == "ketoan", ct);
        admin.DisplayName = "Phạm Quốc Huy";
        admin.EmployeeId = huy.Id;
        ketoan.DisplayName = "Nguyễn Thị Hoa";
        ketoan.EmployeeId = hoa.Id;
        await db.SaveChangesAsync(ct);
        var creditPay = await db.PaymentMethods.FirstOrDefaultAsync(p => p.Code == PaymentMethodCodes.Credit, ct);

        var hongHa = Partner(db, "DT001", "Công ty TNHH Vận tải Hồng Hà", "0107788123",
            "Nguyễn Đức Thành", "024 3662 1188", "Km 5, đường Tam Trinh, Hoàng Mai, Hà Nội", "lienhe@vthongha.vn");
        var ducThanh = Partner(db, "DT002", "DNTN Xe tải Đức Thành", "0904455123",
            "Bùi Đức Thành", "024 3385 2266", "Quốc lộ 1A, Thường Tín, Hà Nội", "ducthanh.xe@gmail.com");
        var dongDo = Partner(db, "DT003", "Công ty CP Logistics Đông Đô", "0201122789",
            "Vũ Thị Hạnh", "0225 383 4455", "Đường Hoàng Văn Thụ, Hồng Bàng, Hải Phòng", "ops@dongdolog.vn");
        await db.SaveChangesAsync(ct);

        var t125 = Type(db, "1.25T");
        var t35 = Type(db, "3.5T");
        var t5 = Type(db, "5T");
        var t8 = Type(db, "8T");
        var t10 = Type(db, "10T");
        var t15 = Type(db, "15T");

        var hung = Driver(db, "TX001", "Nguyễn Văn Hùng", "0903 441 228", hongHa, new DateTime(1986, 5, 9), "031086005209");
        var dung = Driver(db, "TX002", "Phạm Văn Dũng", "0915 662 014", hongHa, new DateTime(1990, 9, 14), "031090009014");
        var cuong = Driver(db, "TX003", "Lê Văn Cường", "0978 330 551", hongHa, new DateTime(1984, 12, 2), "031084012002");
        var nam = Driver(db, "TX004", "Trần Văn Nam", "0966 128 773", ducThanh, new DateTime(1993, 4, 27), "033093004027");
        var phuc = Driver(db, "TX005", "Hoàng Văn Phúc", "0982 557 310", ducThanh, new DateTime(1987, 8, 8), "033087008008");
        var hoaTx = Driver(db, "TX006", "Vũ Đình Hòa", "0934 219 660", dongDo, new DateTime(1989, 2, 16), "031089002016");
        var tai = Driver(db, "TX007", "Đỗ Văn Tài", "0945 881 207", dongDo, new DateTime(1991, 6, 30), "031091006030");
        var quang = Driver(db, "TX008", "Ngô Quang Huy", "0908 776 142", hongHa, new DateTime(1983, 10, 11), "001083010011");
        await db.SaveChangesAsync(ct);

        var xeHung = Vehicle(db, "29C-12345", hongHa, t35, 3.5m);
        var xeDung = Vehicle(db, "29C-23456", hongHa, t5, 5m);
        var xeCuong = Vehicle(db, "29B-34567", hongHa, t8, 8m);
        var xeNam = Vehicle(db, "30H-45678", ducThanh, t125, 1.25m);
        var xePhuc = Vehicle(db, "29A-56789", ducThanh, t10, 10m);
        var xeHoa = Vehicle(db, "15C-67890", dongDo, t5, 5m);
        var xeTai = Vehicle(db, "15C-78901", dongDo, t35, 3.5m);
        var xeQuang = Vehicle(db, "29F-89012", hongHa, t15, 15m);
        await db.SaveChangesAsync(ct);

        var thep = Cust(db, "KH001", "Công ty TNHH TM Thép Bắc Việt", "0108123456",
            "KCN Nội Bài, xã Mai Đình, Sóc Sơn, Hà Nội", "Phạm Thị Mai", "024 3886 2211",
            "muahang@thepbacviet.vn", hn, hoa);
        var nhua = Cust(db, "KH002", "Công ty CP Nhựa An Phát", "0802233445",
            "KCN Cộng Hòa, huyện Chí Linh, Hải Dương", "Lưu Đức Anh", "0220 389 1188",
            "logistics@nhuaanphat.vn", hd, hoa);
        var det = Cust(db, "KH003", "Công ty TNHH Dệt may Thái Hà", "0103344556",
            "KCN Nam Thăng Long, Bắc Từ Liêm, Hà Nội", "Ngô Thanh Hà", "024 3756 4490",
            "kho@detmaythaiha.vn", hn, lan);
        var nong = Cust(db, "KH004", "Công ty TNHH XK Nông sản Đồng Tâm", "0104455667",
            "Số 48, Nguyễn Văn Linh, Long Biên, Hà Nội", "Đặng Minh Tuấn", "024 3872 3300",
            "xuatkhau@nongsandongtam.vn", hn, hoa);
        var dien = Cust(db, "KH005", "Công ty CP Linh kiện Điện tử Sao Việt", "0105566778",
            "KCN Quế Võ 2, huyện Quế Võ, Bắc Ninh", "Trịnh Thu Trang", "0222 363 7788",
            "warehouse@saoviet-el.vn", bn, lan);
        var xd = Cust(db, "KH006", "Công ty TNHH Vật tư Xây dựng Đại Lộc", "0106677889",
            "Km 12, quốc lộ 6, Hà Đông, Hà Nội", "Bùi Văn Lộc", "024 3354 2218",
            "vattu@dailocxd.vn", hn, hoa);
        var cang = Cust(db, "KH007", "Chi nhánh Cảng vụ Giao nhận HP", "0207788990",
            "Khu cảng Đình Vũ, Dương Kinh, Hải Phòng", "Mai Quốc Khánh", "0225 376 2200",
            "giaonhan@cangvu-hp.vn", hp, lan);
        var haLong = Cust(db, "KH008", "Công ty TNHH Kho vận Hạ Long", "0208899001",
            "Cảng Cái Lân, Hạ Long, Quảng Ninh", "Phan Văn Sơn", "0203 362 1188",
            "kho@hlport.vn", qn, lan);
        var mayYen = Cust(db, "KH009", "Xí nghiệp May Ý Yên", "0601122334",
            "KCN Ý Yên, huyện Ý Yên, Nam Định", "Vũ Thị Hương", "0228 386 2200",
            "kho@mayyyen.vn", nd, lan);
        var phoNoi = Cust(db, "KH010", "Công ty TNHH CFS Phố Nối", "0902233445",
            "KCN Phố Nối A, Văn Lâm, Hưng Yên", "Lê Quang Đạt", "0221 394 3300",
            "ops@cfsphonoi.vn", hy, hoa);
        var gangThep = Cust(db, "KH011", "Công ty CP Gang thép Thái Nguyên", "0109900112",
            "KCN Yên Bình, Phổ Yên, Thái Nguyên", "Hoàng Anh Tuấn", "0208 385 7700",
            "kho@tngangthep.vn", tn, hoa);
        var tamDiep = Cust(db, "KH012", "Công ty TNHH VLXD Tam Điệp", "0101011223",
            "KCN Tam Điệp, Ninh Bình", "Đinh Văn Hùng", "0229 377 4411",
            "muahang@vldxtamdiep.vn", nb, hoa);
        var vinhPhucNs = Cust(db, "KH013", "Công ty TNHH Chế biến nông sản Vĩnh Phúc", "0101213141",
            "KCN Khai Quang, TP Vĩnh Yên, Vĩnh Phúc", "Ngô Thị Yến", "0211 386 5500",
            "kho@nsvinhphuc.vn", vp, lan);
        var vangLai = Cust(db, "VL-001", "Chị Nguyễn Thị Bé (vãng lai)", null,
            "12 ngách 15, phố Trần Duy Hưng, Cầu Giấy, Hà Nội", "Nguyễn Thị Bé", "0987 221 334",
            null, hn, minh, walkIn: true);
        await db.SaveChangesAsync(ct);

        var chung = new PriceList
        {
            Code = "BG-2026",
            Name = "Bảng giá chung tuyến Bắc Bộ 2026",
            Description = "Áp dụng khi khách chưa có bảng riêng. Hiệu lực cả năm.",
            EffectiveFrom = new DateTime(today.Year, 1, 1),
            EffectiveTo = new DateTime(today.Year, 12, 31),
            CreatedAt = new DateTime(today.Year, 1, 2)
        };
        var riengThep = new PriceList
        {
            Code = "BG-KH001",
            Name = "Bảng giá Thép Bắc Việt (ưu đãi)",
            Description = "Giảm so với bảng chung trên tuyến Hà Nội — Hải Phòng / Bắc Ninh.",
            CustomerId = thep.Id,
            EffectiveFrom = new DateTime(today.Year, 1, 1),
            EffectiveTo = new DateTime(today.Year, 12, 31),
            CreatedAt = new DateTime(today.Year, 1, 8)
        };
        db.PriceLists.AddRange(chung, riengThep);
        await db.SaveChangesAsync(ct);

        var revChung = new PriceListRevision { PriceListId = chung.Id, EmployeeId = minh.Id, CreatedAt = chung.CreatedAt };
        var revThep = new PriceListRevision { PriceListId = riengThep.Id, EmployeeId = minh.Id, CreatedAt = riengThep.CreatedAt };
        db.PriceListRevisions.AddRange(revChung, revThep);
        await db.SaveChangesAsync(ct);

        void Item(PriceListRevision rev, City pickup, City delivery, VehicleType type, decimal unit, decimal surcharge)
        {
            var route = Pair(pickup, delivery);
            db.PriceListItems.Add(new PriceListItem
            {
                PriceListRevisionId = rev.Id,
                RouteId = route.Id,
                VehicleTypeId = type.Id,
                UnitPrice = unit,
                Surcharge = surcharge
            });
        }

        Item(revChung, hn, hp, t125, 1_800_000, 0);
        Item(revChung, hn, hp, t35, 2_500_000, 100_000);
        Item(revChung, hn, hp, t5, 3_200_000, 150_000);
        Item(revChung, hn, hp, t8, 4_200_000, 200_000);
        Item(revChung, hn, hp, t10, 5_500_000, 250_000);
        Item(revChung, hn, hp, t15, 7_200_000, 300_000);
        Item(revChung, hn, bn, t125, 1_200_000, 0);
        Item(revChung, hn, bn, t35, 1_800_000, 50_000);
        Item(revChung, hn, bn, t5, 2_200_000, 80_000);
        Item(revChung, hn, hd, t35, 2_100_000, 80_000);
        Item(revChung, hn, hd, t5, 2_700_000, 100_000);
        Item(revChung, hn, hy, t125, 1_050_000, 0);
        Item(revChung, hn, hy, t35, 1_600_000, 40_000);
        Item(revChung, hn, qn, t5, 4_800_000, 200_000);
        Item(revChung, hn, qn, t10, 6_800_000, 300_000);
        Item(revChung, hn, tn, t35, 2_400_000, 100_000);
        Item(revChung, hn, tn, t10, 5_500_000, 250_000);
        Item(revChung, hn, vp, t35, 1_700_000, 50_000);
        Item(revChung, hn, nd, t5, 3_600_000, 150_000);
        Item(revChung, hn, nb, t5, 3_900_000, 150_000);
        Item(revChung, hn, nb, t8, 4_200_000, 200_000);
        Item(revChung, hn, hn, t125, 1_050_000, 0);
        Item(revChung, hp, hn, t35, 2_450_000, 100_000);
        Item(revChung, hp, hn, t5, 3_100_000, 150_000);
        Item(revChung, bn, hn, t35, 1_750_000, 50_000);
        Item(revChung, bn, hn, t125, 1_200_000, 0);
        Item(revChung, bn, hd, t35, 2_100_000, 80_000);
        Item(revChung, hd, hn, t5, 2_700_000, 100_000);
        Item(revChung, hd, hy, t35, 1_600_000, 40_000);

        Item(revThep, hn, hp, t35, 2_350_000, 80_000);
        Item(revThep, hn, hp, t5, 3_000_000, 120_000);
        Item(revThep, hn, hp, t8, 3_950_000, 180_000);
        Item(revThep, hn, bn, t35, 1_700_000, 40_000);
        Item(revThep, hn, bn, t5, 2_050_000, 60_000);
        await db.SaveChangesAsync(ct);

        var khoNoiBai = "Kho A2, KCN Nội Bài, Sóc Sơn, Hà Nội";
        var cangHp = "Cảng Hoàng Diệu / Đình Vũ, Hải Phòng";
        var kcnQueVo = "Nhà máy B2, KCN Quế Võ 2, Bắc Ninh";
        var kcnPhoNoi = "Kho CFS, KCN Phố Nối A, Hưng Yên";
        var nhaMayHd = "Nhà máy An Phát, KCN Cộng Hòa, Hải Dương";
        var caiLan = "Cảng Cái Lân, Hạ Long, Quảng Ninh";
        var namThangLong = "Xưởng cắt may, KCN Nam Thăng Long, Hà Nội";
        var kcnYenBinh = "KCN Yên Bình, Phổ Yên, Thái Nguyên";
        var kcnTamDiep = "KCN Tam Điệp, Ninh Bình";
        var kcnKhaiQuang = "KCN Khai Quang, Vĩnh Yên, Vĩnh Phúc";
        var khoYen = "Xí nghiệp May Ý Yên, Nam Định";

        var n = 1;
        string NextCode() => (n++).ToString("000");

        DispatchOrder Add(
            DateTime pickup,
            Customer bill,
            Customer sender,
            Customer receiver,
            City from,
            City to,
            string pickupAddr,
            string deliveryAddr,
            Vehicle vehicle,
            Driver driver,
            VehicleType type,
            decimal unit,
            decimal surcharge,
            decimal extra,
            DispatchStatus status,
            bool reconciled,
            string goods,
            int packages,
            string? notes = null,
            bool deliveryNote = false,
            bool invoice = false)
        {
            var route = Pair(from, to);
            var origin = locByCityId[from.Id];
            var dest = locByCityId[to.Id];
            var order = new DispatchOrder
            {
                Code = NextCode(),
                CreatedAt = pickup.Date.AddHours(-14).AddMinutes(20),
                CreatedByUserId = admin.Id,
                Status = status,
                ReconciliationStatus = reconciled ? ReconciliationStatus.Reconciled : ReconciliationStatus.Pending,
                ReconciledAt = reconciled ? pickup.Date.AddDays(1).AddHours(16) : null,
                ReconciledByUserId = reconciled ? ketoan.Id : null,
                CustomerId = bill.Id,
                SenderCustomerId = sender.Id,
                SenderName = sender.Name,
                SenderPhone = sender.Phone,
                SenderAddress = sender.Address,
                SenderTaxCode = sender.TaxCode,
                ReceiverCustomerId = receiver.Id,
                ReceiverName = receiver.Name,
                ReceiverPhone = receiver.Phone,
                ReceiverAddress = receiver.Address,
                ReceiverTaxCode = receiver.TaxCode,
                PickupAt = pickup,
                PickupAddress = pickupAddr,
                DeliveryAddress = deliveryAddr,
                RouteId = route.Id,
                VehicleId = vehicle.Id,
                DriverId = driver.Id,
                VehicleTypeId = type.Id,
                EmployeeId = minh.Id,
                PaymentMethodId = creditPay?.Id,
                BillingYear = pickup.Year,
                BillingMonth = pickup.Month,
                UnitPrice = unit,
                Surcharge = surcharge,
                ExtraCost = extra,
                Notes = notes
            };
            order.RecalculateTotal();
            order.AmountInWords = VietnameseAmountWords.ToWords(order.TotalAmount);
            order.ReplaceStops([(origin.Id, origin.Name), (dest.Id, dest.Name)]);
            order.Lines.Add(new DispatchOrderLine
            {
                LineNumber = 1,
                GoodsName = goods,
                PackageCount = packages,
                Route = $"{from.Name} → {to.Name}",
                Kilometers = Km(from.Code, to.Code),
                Notes = extra > 0 ? "Có phát sinh" : null
            });
            db.DispatchOrders.Add(order);
            if (deliveryNote || reconciled)
                order.Documents.Add(Placeholder(DispatchDocumentKind.DeliveryNote, "BBGH.pdf", ketoan.Id, pickup.AddHours(10)));
            if (status != DispatchStatus.Issued)
                order.Documents.Add(Placeholder(DispatchDocumentKind.DispatchOrder, "LDX.pdf", admin.Id, pickup.AddHours(-2)));
            if (invoice)
                order.Documents.Add(Placeholder(DispatchDocumentKind.Invoice, "HoaDon.pdf", ketoan.Id, pickup.AddDays(2)));
            return order;
        }

        DateTime Lm(int day, int hour, int minute = 0) =>
            new(lastMonth.Year, lastMonth.Month, Math.Min(day, lastMonthEnd.Day), hour, minute, 0);

        DateTime Tm(int day, int hour, int minute = 0) =>
            new(thisMonth.Year, thisMonth.Month, Math.Clamp(day, 1, daysThis), hour, minute, 0);

        // Tháng trước — đã xong chuyến, đã đối soát, đủ biên bản → bảng kê
        Add(Lm(3, 5, 30), thep, thep, cang, hn, hp, khoNoiBai, cangHp, xeDung, dung, t5, 3_000_000, 120_000, 0,
            DispatchStatus.Completed, true, "Tôn cuộn mạ kẽm", 18, invoice: true);
        Add(Lm(5, 6, 0), thep, thep, cang, hn, hp, khoNoiBai, cangHp, xeHung, hung, t35, 2_350_000, 80_000, 0,
            DispatchStatus.Completed, true, "Thép hộp 50x50", 40, invoice: true);
        Add(Lm(8, 4, 45), thep, thep, dien, hn, bn, khoNoiBai, kcnQueVo, xeHung, hung, t35, 1_700_000, 40_000, 0,
            DispatchStatus.Completed, true, "Thép cây D10", 25);
        Add(Lm(12, 5, 15), thep, thep, cang, hn, hp, khoNoiBai, cangHp, xeCuong, cuong, t8, 3_950_000, 180_000, 150_000,
            DispatchStatus.Completed, true, "Tôn cuộn khổ 1200", 12, "Phát sinh cầu đường + chờ bốc 1.5 giờ");
        Add(Lm(18, 6, 20), thep, thep, cang, hn, hp, khoNoiBai, cangHp, xeDung, dung, t5, 3_000_000, 120_000, 0,
            DispatchStatus.Completed, true, "Thép hình I 200", 16);
        Add(Lm(22, 5, 0), thep, thep, dien, hn, bn, khoNoiBai, kcnQueVo, xeDung, dung, t5, 2_050_000, 60_000, 0,
            DispatchStatus.Completed, true, "Tôn lạnh", 22);

        Add(Lm(4, 7, 0), det, det, nhua, hn, hd, namThangLong, nhaMayHd, xeTai, tai, t35, 2_100_000, 80_000, 0,
            DispatchStatus.Completed, true, "Vải cuộn polyester", 30);
        Add(Lm(10, 6, 30), det, det, nhua, hn, hd, namThangLong, nhaMayHd, xeHoa, hoaTx, t5, 2_700_000, 100_000, 0,
            DispatchStatus.Completed, true, "Vải suc, phụ liệu may", 48);
        Add(Lm(16, 8, 0), det, det, vangLai, hn, hn, namThangLong, vangLai.Address ?? "", xeNam, nam, t125, 1_050_000, 0, 0,
            DispatchStatus.Completed, true, "Thùng carton mẫu", 8, "Giao nội thành — khách vãng lai nhận hộ");
        Add(Lm(24, 5, 40), det, det, nhua, hn, hd, namThangLong, nhaMayHd, xeTai, tai, t35, 2_100_000, 80_000, 80_000,
            DispatchStatus.Completed, true, "Chỉ may + vải kẹp", 20, "Phụ phí giao ca đêm");

        Add(Lm(7, 5, 10), dien, dien, thep, bn, hn, kcnQueVo, khoNoiBai, xeHung, hung, t35, 1_750_000, 50_000, 0,
            DispatchStatus.Completed, true, "Linh kiện PCB (trả kho)", 14);
        Add(Lm(20, 9, 0), xd, xd, nong, hn, hn, xd.Address ?? "", nong.Address ?? "", xeNam, nam, t125, 1_050_000, 0, 0,
            DispatchStatus.Completed, true, "Xi măng bao (nội thành)", 50);

        // Tháng này — đã đối soát (dashboard + không vào bảng kê vì chưa Generate tháng này)
        Add(Tm(Math.Min(2, past), 5, 20), thep, thep, cang, hn, hp, khoNoiBai, cangHp, xeDung, dung, t5, 3_000_000, 120_000, 0,
            DispatchStatus.Completed, true, "Tôn cuộn", 16, deliveryNote: true);
        Add(Tm(Math.Min(4, past), 6, 10), thep, thep, cang, hn, hp, khoNoiBai, cangHp, xeHung, hung, t35, 2_350_000, 80_000, 0,
            DispatchStatus.Completed, true, "Thép hộp", 36, deliveryNote: true);
        Add(Tm(Math.Min(6, past), 4, 50), nhua, nhua, det, hd, hn, nhaMayHd, namThangLong, xeHoa, hoaTx, t5, 2_700_000, 100_000, 0,
            DispatchStatus.Completed, true, "Hạt nhựa PP", 28, deliveryNote: true);
        Add(Tm(Math.Min(1, past), 6, 15), thep, thep, cang, hn, hp, khoNoiBai, cangHp, xeQuang, quang, t15, 7_200_000, 300_000, 400_000,
            DispatchStatus.Completed, true, "Tôn cuộn siêu trường", 6, "Xe 15 tấn — phát sinh đăng kiểm tạm", deliveryNote: true);

        // Tháng này — hoàn thành + có BBGH → hàng đợi đối soát
        Add(Tm(Math.Min(8, past), 5, 30), thep, thep, cang, hn, hp, khoNoiBai, cangHp, xeCuong, cuong, t8, 3_950_000, 180_000, 0,
            DispatchStatus.Completed, false, "Tôn cuộn khổ lớn", 10, deliveryNote: true);
        Add(Tm(Math.Min(9, past), 6, 0), dien, dien, nhua, bn, hd, kcnQueVo, nhaMayHd, xeTai, tai, t35, 2_100_000, 80_000, 0,
            DispatchStatus.Completed, false, "Linh kiện đóng thùng", 22, deliveryNote: true);
        Add(Tm(Math.Min(11, past), 5, 45), nong, nong, cang, hn, hp, nong.Address ?? khoNoiBai, cangHp, xeDung, dung, t5, 3_200_000, 150_000, 200_000,
            DispatchStatus.Completed, false, "Gạo xuất khẩu (bao 50kg)", 200, "Phát sinh lưu container 1 ngày", deliveryNote: true);
        Add(Tm(Math.Min(13, past), 7, 15), xd, xd, haLong, hn, qn, xd.Address ?? "", caiLan, xePhuc, phuc, t10, 6_800_000, 300_000, 0,
            DispatchStatus.Completed, false, "Sắt thép xây dựng", 18, deliveryNote: true);
        Add(Tm(Math.Min(15, past), 6, 30), det, det, mayYen, hn, nd, namThangLong, khoYen, xeHoa, hoaTx, t5, 3_600_000, 150_000, 0,
            DispatchStatus.Completed, false, "Thành phẩm áo jacket", 60, deliveryNote: true);
        Add(Tm(Math.Min(16, past), 13, 30), cang, cang, thep, hp, hn, cangHp, khoNoiBai, xeHoa, hoaTx, t5, 3_100_000, 150_000, 0,
            DispatchStatus.Completed, false, "Hàng nhập trả kho Nội Bài", 11, "Chuyến chiều — đã có BBGH", deliveryNote: true);
        Add(Tm(Math.Min(17, past), 5, 50), thep, thep, tamDiep, hn, nb, khoNoiBai, kcnTamDiep, xeCuong, cuong, t8, 4_200_000, 200_000, 0,
            DispatchStatus.Completed, false, "Tôn lợp nhà xưởng", 9, deliveryNote: true);

        // Tháng này — hoàn thành nhưng thiếu BBGH (dashboard chờ chứng từ; đối soát thấy THIẾU)
        Add(Tm(Math.Min(10, past), 5, 0), thep, thep, cang, hn, hp, khoNoiBai, cangHp, xeHung, hung, t35, 2_350_000, 80_000, 0,
            DispatchStatus.Completed, false, "Thép cây D12", 20, "Thiếu biên bản giao hàng — tài xế chưa nộp");
        Add(Tm(Math.Min(12, past), 8, 20), dien, dien, thep, bn, hn, kcnQueVo, khoNoiBai, xeNam, nam, t125, 1_200_000, 0, 0,
            DispatchStatus.Completed, false, "Hàng mẫu QC", 4, "Chờ scan BBGH");
        Add(Tm(Math.Min(14, past), 6, 5), nhua, nhua, phoNoi, hd, hy, nhaMayHd, kcnPhoNoi, xeTai, tai, t35, 1_600_000, 40_000, 0,
            DispatchStatus.Completed, false, "Bao bì nhựa", 90, "Khách chưa ký biên bản");

        Add(Tm(Math.Min(today.Day + 1, daysThis), 5, 30),
            thep, thep, cang, hn, hp, khoNoiBai, cangHp, xeDung, dung, t5, 3_000_000, 120_000, 0,
            DispatchStatus.Issued, false, "Tôn cuộn (lệnh mai)", 14, "Lấy hàng ca sớm");
        Add(Tm(Math.Min(today.Day + 2, daysThis), 6, 0),
            vangLai, vangLai, det, hn, hn, vangLai.Address ?? "", namThangLong, xeNam, nam, t125, 1_050_000, 0, 0,
            DispatchStatus.Issued, false, "Vải vụn / hàng lẻ", 6, "Khách vãng lai — thu tiền mặt khi giao");
        Add(Tm(Math.Min(today.Day + 3, daysThis), 4, 40),
            xd, xd, gangThep, hn, tn, xd.Address ?? "", kcnYenBinh, xePhuc, phuc, t10, 5_500_000, 250_000, 0,
            DispatchStatus.Issued, false, "Gạch block + xi măng", 32);

        var locked = Add(Tm(Math.Min(3, past), 9, 0), nong, nong, vinhPhucNs, hn, vp, nong.Address ?? "", kcnKhaiQuang,
            xeHung, hung, t35, 1_700_000, 50_000, 0,
            DispatchStatus.Completed, true, "Nông sản sấy", 24, "Lệnh khóa sau đối soát — không sửa cước", deliveryNote: true);
        locked.Status = DispatchStatus.Locked;

        await db.SaveChangesAsync(ct);

        WritePlaceholderFiles(db);
        await db.SaveChangesAsync(ct);

        var firstThep = await db.DispatchOrders.OrderBy(o => o.Id).FirstAsync(ct);
        db.ChangeLogs.AddRange(
            new ChangeLog
            {
                EntityName = "DispatchOrder",
                EntityId = firstThep.Id,
                Action = "Create",
                Summary = "Tạo lệnh điều xe",
                UserId = admin.Id,
                ChangedAt = firstThep.CreatedAt
            },
            new ChangeLog
            {
                EntityName = "DispatchOrder",
                EntityId = firstThep.Id,
                Action = "Reconcile",
                Summary = "Đã đối soát",
                UserId = ketoan.Id,
                ChangedAt = firstThep.ReconciledAt ?? lastMonth.AddDays(4)
            },
            new ChangeLog
            {
                EntityName = "DispatchOrder",
                EntityId = locked.Id,
                Action = "Reconcile",
                Summary = "Đã đối soát",
                UserId = ketoan.Id,
                ChangedAt = locked.ReconciledAt ?? today
            },
            new ChangeLog
            {
                EntityName = "DispatchOrder",
                EntityId = locked.Id,
                Action = "Lock",
                Summary = "Khóa lệnh",
                UserId = admin.Id,
                ChangedAt = today.AddDays(-Math.Min(1, today.Day - 1)).AddHours(17)
            });
        await db.SaveChangesAsync(ct);

        await BuildStatementAsync(db, thep, lastMonth.Year, lastMonth.Month, ct);
        await BuildStatementAsync(db, det, lastMonth.Year, lastMonth.Month, ct);

        SetSeq(db, "dispatch-order", n - 1);
        SetSeq(db, "walk-in-customer", 1);
        SetSeq(db, "freight-statement", await db.FreightStatements.CountAsync(ct));

        var company = await db.Companies.FirstAsync(ct);
        company.Name = "Công ty TNHH dịch vụ vận tải và thương mại Hà Minh Anh";
        company.Address = "Số 27, phố Nguyễn Khoái, phường Bạch Đằng, quận Hai Bà Trưng, Hà Nội";
        company.Phone = "024 3978 1122";
        company.TaxCode = "0105678910";
        company.Email = "vanhanh@haminhanh.vn";
        company.Website = "www.haminhanh.vn";
        company.Bank = "Vietcombank — CN Hà Nội — STK 0011001234567";
        await db.SaveChangesAsync(ct);
    }

    private static City City(HmaDbContext db, string code, string name, string? desc)
    {
        var row = new City { Code = code, Name = name, Description = desc };
        db.Cities.Add(row);
        return row;
    }

    private static Department Dept(HmaDbContext db, string code, string name)
    {
        var row = new Department { Code = code, Name = name };
        db.Departments.Add(row);
        return row;
    }

    private static JobTitle Title(HmaDbContext db, string code, string name)
    {
        var row = new JobTitle { Code = code, Name = name };
        db.JobTitles.Add(row);
        return row;
    }

    private static Employee Emp(HmaDbContext db, string code, string name, string mobile,
        Department dept, JobTitle title, string address, DateTime birth, string cccd)
    {
        var row = new Employee
        {
            Code = code,
            Name = name,
            Mobile = mobile,
            Phone = mobile,
            DepartmentId = dept.Id,
            JobTitleId = title.Id,
            Address = address,
            BirthDate = birth,
            IdentityNumber = cccd,
            UpdatedAt = DateTime.Today
        };
        db.Employees.Add(row);
        return row;
    }

    private static Partner Partner(HmaDbContext db, string code, string name, string tax,
        string contact, string phone, string address, string email)
    {
        var row = new Partner
        {
            Code = code, Name = name, TaxCode = tax, ContactName = contact,
            Phone = phone, Address = address, Email = email, OperatingFeePercent = 8
        };
        db.Partners.Add(row);
        return row;
    }

    private static Driver Driver(HmaDbContext db, string code, string name, string phone,
        Partner partner, DateTime birth, string cccd)
    {
        var row = new Driver
        {
            Code = code, Name = name, Phone = phone, PartnerId = partner.Id,
            BirthDate = birth, IdentityNumber = cccd
        };
        db.Drivers.Add(row);
        return row;
    }

    private static Vehicle Vehicle(HmaDbContext db, string plate, Partner partner, VehicleType type, decimal tonnage)
    {
        var row = new Vehicle
        {
            PlateNumber = plate, PartnerId = partner.Id, VehicleTypeId = type.Id, Tonnage = tonnage
        };
        db.Vehicles.Add(row);
        return row;
    }

    private static Customer Cust(HmaDbContext db, string code, string name, string? tax, string address,
        string contact, string phone, string? email, City city, Employee accountant, bool walkIn = false)
    {
        var row = new Customer
        {
            Code = code,
            Name = name,
            TaxCode = tax,
            Address = address,
            ContactName = contact,
            Phone = phone,
            Email = email,
            CityId = city.Id,
            AccountantEmployeeId = accountant.Id,
            IsWalkIn = walkIn,
            UpdatedAt = DateTime.Today.AddDays(-3)
        };
        db.Customers.Add(row);
        return row;
    }

    private static VehicleType Type(HmaDbContext db, string code) =>
        db.VehicleTypes.Local.FirstOrDefault(t => t.Code == code)
        ?? db.VehicleTypes.First(t => t.Code == code);

    private static decimal Km(string from, string to) => (from, to) switch
    {
        ("HN", "HP") or ("HP", "HN") => 102,
        ("HN", "BN") or ("BN", "HN") => 32,
        ("HN", "HD") or ("HD", "HN") => 58,
        ("HN", "HY") or ("HY", "HN") => 45,
        ("HN", "QN") => 165,
        ("HN", "TN") => 75,
        ("HN", "VP") => 62,
        ("HN", "ND") => 90,
        ("HN", "NB") => 95,
        ("HN", "HN") => 18,
        ("BN", "HD") => 48,
        ("HD", "HY") => 28,
        _ => 40
    };

    private static DispatchDocument Placeholder(DispatchDocumentKind kind, string fileName, int userId, DateTime uploadedAt) =>
        new()
        {
            Kind = kind,
            FileName = fileName,
            StoredPath = "",
            UploadedAt = uploadedAt,
            UploadedByUserId = userId
        };

    private static void WritePlaceholderFiles(HmaDbContext db)
    {
        var root = db.Parameters.Local.FirstOrDefault(p => p.Key == "DocumentStorePath")?.Value?.Trim()
                   ?? db.Parameters.FirstOrDefault(p => p.Key == "DocumentStorePath")?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(root))
            root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Hma", "Documents");

        foreach (var doc in db.DispatchDocuments.Local.Where(d => string.IsNullOrEmpty(d.StoredPath)))
        {
            var order = doc.DispatchOrder ?? db.DispatchOrders.Local.First(o => o.Id == doc.DispatchOrderId);
            var key = FileStorageKey.ForDispatchDocument(order.Id, doc.FileName);
            var dest = Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            var title = doc.Kind switch
            {
                DispatchDocumentKind.DeliveryNote => "BIÊN BẢN GIAO HÀNG (mẫu diễn tập)",
                DispatchDocumentKind.Invoice => "HÓA ĐƠN / CHỨNG TỪ (mẫu diễn tập)",
                _ => "LỆNH ĐIỀU XE (mẫu diễn tập)"
            };
            File.WriteAllText(dest,
                $"{title}\r\nSố lệnh: {order.Code}\r\nKhách: {order.SenderName}\r\nTuyến: {order.RouteLabel}\r\nFile diễn tập — không dùng để đối chiếu pháp lý.\r\n");
            doc.StoredPath = key;
            doc.DispatchOrderId = order.Id;
        }
    }

    private static async Task BuildStatementAsync(HmaDbContext db, Customer customer, int year, int month, CancellationToken ct)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var orders = await db.DispatchOrders
            .Include(d => d.Vehicle)
            .Include(d => d.Driver)
            .Include(d => d.VehicleType)
            .Include(d => d.Stops)
            .Include(d => d.Route)
            .Include(d => d.Documents)
            .Where(d => d.CustomerId == customer.Id
                        && d.PickupAt >= start && d.PickupAt < end
                        && d.Status == DispatchStatus.Completed
                        && d.ReconciliationStatus == ReconciliationStatus.Reconciled
                        && d.Documents.Any(doc => doc.Kind == DispatchDocumentKind.DeliveryNote))
            .OrderBy(d => d.PickupAt)
            .ToListAsync(ct);
        if (orders.Count == 0)
            return;

        var next = await db.FreightStatements.CountAsync(ct) + 1;
        var statement = new FreightStatement
        {
            Code = next.ToString("000"),
            CustomerId = customer.Id,
            Year = year,
            Month = month,
            CreatedAt = end.AddDays(2),
            VatRate = 10
        };
        foreach (var o in orders)
        {
            statement.Lines.Add(new FreightStatementLine
            {
                DispatchOrderId = o.Id,
                TripDate = o.PickupAt,
                DispatchCode = o.Code,
                Route = o.RouteLabel,
                PlateNumber = o.Vehicle?.PlateNumber,
                Tonnage = o.VehicleType?.Name,
                DriverName = o.Driver?.Name,
                UnitPrice = o.UnitPrice,
                Surcharge = o.Surcharge,
                ExtraCost = o.ExtraCost,
                LineTotal = o.TotalAmount,
                Notes = o.Notes
            });
        }

        statement.TripCount = statement.Lines.Count;
        statement.FreightTotal = statement.Lines.Sum(l => l.UnitPrice);
        statement.SurchargeTotal = statement.Lines.Sum(l => l.Surcharge);
        statement.ExtraCostTotal = statement.Lines.Sum(l => l.ExtraCost);
        statement.GrandTotal = statement.Lines.Sum(l => l.LineTotal);
        statement.VatAmount = Math.Round(statement.GrandTotal * statement.VatRate / 100m, 2);
        statement.TotalWithVat = statement.GrandTotal + statement.VatAmount;
        statement.Notes = $"Tổng hợp từ lệnh đã đối soát tháng {month:00}/{year}.";
        db.FreightStatements.Add(statement);
        await db.SaveChangesAsync(ct);
    }

    private static void SetSeq(HmaDbContext db, string key, int last)
    {
        var seq = db.Sequences.Local.FirstOrDefault(s => s.Key == key)
                  ?? db.Sequences.First(s => s.Key == key);
        if (seq.LastValue < last)
            seq.LastValue = last;
    }
}
