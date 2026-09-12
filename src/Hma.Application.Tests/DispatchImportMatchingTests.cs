using Hma.Application.Features.Dispatching.Import;
using Hma.Domain.Entities;

namespace Hma.Application.Tests;

public class DispatchImportMatchingTests
{
    [Fact]
    public void MatchCustomer_is_case_insensitive_on_code()
    {
        var items = new[] { new Customer { Code = "KH001", Name = "Thép" } };
        Assert.Equal("Thép", DispatchImportMatching.MatchCustomer(items, " kh001 ")?.Name);
        Assert.Null(DispatchImportMatching.MatchCustomer(items, "KH999"));
        Assert.Null(DispatchImportMatching.MatchCustomer(items, " "));
    }

    [Fact]
    public void MatchLocation_accepts_code_or_vietnamese_name()
    {
        var items = new[] { new Location { Code = "HN", Name = "Hà Nội" }, new Location { Code = "HP", Name = "Hải Phòng" } };
        Assert.Equal("HN", DispatchImportMatching.MatchLocation(items, "Hà Nội")?.Code);
        Assert.Equal("HP", DispatchImportMatching.MatchLocation(items, "hp")?.Code);
    }

    [Fact]
    public void MatchCustomer_uses_alias_after_code_miss()
    {
        var customers = new[] { new Customer { Id = 1, Code = "KH001", Name = "Thép" } };
        var aliases = new[] { new CustomerAlias { Alias = "TEC", CustomerId = 1, Customer = customers[0] } };
        Assert.Equal("Thép", DispatchImportMatching.MatchCustomer(customers, aliases, " tec ")?.Name);
        Assert.Equal("Thép", DispatchImportMatching.MatchCustomer(customers, aliases, "KH001")?.Name);
        Assert.Null(DispatchImportMatching.MatchCustomer(customers, aliases, "KMG"));
        Assert.Null(DispatchImportMatching.MatchCustomer(customers, aliases, " "));
    }

    [Fact]
    public void MatchLocation_uses_alias_after_catalog_miss_and_respects_kind()
    {
        var noiBai = new Location { Id = 10, Code = "NB", Name = "Nội Bài" };
        var haiPhong = new Location { Id = 20, Code = "HP", Name = "Hải Phòng" };
        var locations = new[] { noiBai, haiPhong };
        var aliases = new[]
        {
            new LocationAlias { Alias = "nb", LocationId = 10, Location = noiBai, Kind = LocationAliasKind.Both },
            new LocationAlias { Alias = "cảng HP", LocationId = 20, Location = haiPhong, Kind = LocationAliasKind.Delivery }
        };

        Assert.Equal("NB", DispatchImportMatching.MatchLocation(locations, aliases, "  nb  ", LocationMatchRole.Pickup)?.Code);
        Assert.Equal("NB", DispatchImportMatching.MatchLocation(locations, aliases, "nb", LocationMatchRole.Delivery)?.Code);
        Assert.Equal("HP", DispatchImportMatching.MatchLocation(locations, aliases, "cảng   HP", LocationMatchRole.Delivery)?.Code);
        Assert.Null(DispatchImportMatching.MatchLocation(locations, aliases, "cảng HP", LocationMatchRole.Pickup));
        Assert.Equal("HP", DispatchImportMatching.MatchLocation(locations, aliases, "Hải Phòng", LocationMatchRole.Pickup)?.Code);
        Assert.Null(DispatchImportMatching.MatchLocation(locations, aliases, "QV", LocationMatchRole.Pickup));
    }

    [Fact]
    public void ResolveRoute_prefers_full_string_alias_then_composes_points()
    {
        var nb = new Location { Id = 1, Code = "NB", Name = "Nội Bài" };
        var hp = new Location { Id = 2, Code = "HP", Name = "Hải Phòng" };
        var qn = new Location { Id = 3, Code = "QN", Name = "Hạ Long" };
        var two = new Route { Id = 10, Code = "NB-HP", Name = "Nội Bài → Hải Phòng", Fingerprint = "1-2" };
        var three = new Route { Id = 11, Code = "NB-HP-QN", Name = "Nội Bài → Hải Phòng → Hạ Long", Fingerprint = "1-2-3" };
        var routeAliases = new[] { new RouteAlias { Alias = "chuyển kho nb", RouteId = 10, Route = two } };
        var locAliases = new[]
        {
            new LocationAlias { Alias = "nb", LocationId = 1, Location = nb, Kind = LocationAliasKind.Both },
            new LocationAlias { Alias = "hp", LocationId = 2, Location = hp, Kind = LocationAliasKind.Both },
            new LocationAlias { Alias = "hl", LocationId = 3, Location = qn, Kind = LocationAliasKind.Both }
        };

        var byAlias = DispatchImportMatching.ResolveRoute("chuyển kho nb", null, null, [two, three], routeAliases, [nb, hp, qn], locAliases);
        Assert.Same(two, byAlias.Route);
        Assert.Empty(byAlias.Errors);

        var composed = DispatchImportMatching.ResolveRoute("nb - hp - hl", null, null, [two, three], routeAliases, [nb, hp, qn], locAliases);
        Assert.Same(three, composed.Route);
        Assert.Empty(composed.Errors);

        var missing = DispatchImportMatching.ResolveRoute("nb - qn", null, null, [two, three], routeAliases, [nb, hp, qn], locAliases);
        Assert.Null(missing.Route);
        Assert.Contains(missing.Errors, e => e.Contains("Chưa có tuyến", StringComparison.Ordinal));

        var fromColumns = DispatchImportMatching.ResolveRoute(null, "nb", "hp", [two, three], routeAliases, [nb, hp, qn], locAliases);
        Assert.Same(two, fromColumns.Route);
        Assert.Empty(fromColumns.Errors);
    }

    [Fact]
    public void MatchVehicle_ignores_spaces_in_plate()
    {
        var items = new[] { new Vehicle { PlateNumber = "29A-12345", PartnerId = 3 } };
        Assert.Equal(3, DispatchImportMatching.MatchVehicle(items, "29A - 12345")?.PartnerId);
        Assert.Equal(3, DispatchImportMatching.MatchVehicle(items, "29A12345")?.PartnerId);
        Assert.Equal(3, DispatchImportMatching.MatchVehicle(items, "29a12345")?.PartnerId);
        Assert.Null(DispatchImportMatching.MatchVehicle(items, "30A-00000"));
    }

    [Fact]
    public void MatchVehicle_uses_plate_alias_when_canonical_plate_differs()
    {
        var vehicle = new Vehicle { Id = 9, PlateNumber = "29C-23456", PartnerId = 4 };
        var aliases = new[] { new VehicleAlias { Alias = "29C23456", VehicleId = 9, Vehicle = vehicle } };
        Assert.Equal(4, DispatchImportMatching.MatchVehicle([vehicle], aliases, "29c23456")?.PartnerId);
        Assert.Equal(4, DispatchImportMatching.MatchVehicle([vehicle], aliases, "29C-23456")?.PartnerId);
        Assert.Null(DispatchImportMatching.MatchVehicle([vehicle], aliases, "30H-45678"));
    }

    [Fact]
    public void MatchDriver_falls_back_to_same_partner_when_name_blank()
    {
        var items = new[]
        {
            new Driver { Name = "An", Code = "TX1", PartnerId = 1 },
            new Driver { Name = "Bình", Code = "TX2", PartnerId = 2 }
        };
        Assert.Equal("Bình", DispatchImportMatching.MatchDriver(items, null, 2)?.Name);
        Assert.Equal("An", DispatchImportMatching.MatchDriver(items, "TX1", 2)?.Name);
        Assert.Null(DispatchImportMatching.MatchDriver(items, "Không có", 2));
    }

    [Fact]
    public void MatchPayment_defaults_to_credit_when_blank()
    {
        var items = new[]
        {
            new PaymentMethod { Code = PaymentMethodCodes.DriverCollect, Name = "Lái xe thu" },
            new PaymentMethod { Code = PaymentMethodCodes.Credit, Name = "Trả sau" },
            new PaymentMethod { Code = PaymentMethodCodes.DispatcherCollect, Name = "Điều hành thu" }
        };
        Assert.Equal(PaymentMethodCodes.Credit, DispatchImportMatching.MatchPayment(items, null)?.Code);
        Assert.Equal(PaymentMethodCodes.DriverCollect, DispatchImportMatching.MatchPayment(items, "Lái xe thu")?.Code);
        Assert.Equal(PaymentMethodCodes.DispatcherCollect, DispatchImportMatching.MatchPayment(items, "Điều hành thu")?.Code);
        Assert.Equal(PaymentMethodCodes.DispatcherCollect, DispatchImportMatching.MatchPayment(items, "dieu-hanh-thu")?.Code);
    }

    [Fact]
    public void ParseDate_accepts_common_excel_formats()
    {
        Assert.Equal(new DateTime(2026, 8, 31), DispatchImportMatching.ParseDate("31/08/2026"));
        Assert.Equal(new DateTime(2026, 8, 31), DispatchImportMatching.ParseDate("2026-08-31"));
        var ex = Assert.Throws<InvalidOperationException>(() => DispatchImportMatching.ParseDate(""));
        Assert.Contains("Thiếu ngày chạy", ex.Message, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => DispatchImportMatching.ParseDate("không phải ngày"));
    }

    [Fact]
    public void ParseMoney_empty_is_zero_and_rejects_negative()
    {
        Assert.Equal(0, DispatchImportMatching.ParseMoney(" ", "Cước"));
        Assert.True(DispatchImportMatching.TryParseMoney("2,430,000", out var amount) && amount == 2_430_000);
        Assert.True(DispatchImportMatching.TryParseMoney("1.080.000", out amount) && amount == 1_080_000);
        Assert.True(DispatchImportMatching.TryParseMoney("1.500k", out amount) && amount == 1_500_000);
        var ex = Assert.Throws<InvalidOperationException>(() => DispatchImportMatching.ParseMoney("-1", "Cước"));
        Assert.Contains("không được âm", ex.Message, StringComparison.Ordinal);
    }
}
