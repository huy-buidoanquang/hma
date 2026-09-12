using Hma.Domain.Entities;
using Hma.Domain.Enums;
using Hma.Domain.Rules;

namespace Hma.Domain.Tests;

public class PriceFluctuationRulesTests
{
    private static readonly DateTime AsOf = new(2026, 9, 12);

    [Theory]
    [InlineData(10, 1000000, 100000)]
    [InlineData(-10, 1000000, -100000)]
    [InlineData(2.555, 1000, 25.55)]
    public void Percentage_is_applied_to_unit_price(decimal value, decimal unitPrice, decimal expected)
    {
        var fluctuation = Valid(PriceFluctuationType.Percentage, value);

        var amount = PriceFluctuationRules.CalculateAmount(unitPrice, fluctuation);

        Assert.Equal(expected, amount);
    }

    [Theory]
    [InlineData(100000, 100000)]
    [InlineData(-100000, -100000)]
    public void Fixed_amount_is_applied_per_quote(decimal value, decimal expected)
    {
        var fluctuation = Valid(PriceFluctuationType.FixedAmount, value);

        var amount = PriceFluctuationRules.CalculateAmount(1000000, fluctuation);

        Assert.Equal(expected, amount);
    }

    [Fact]
    public void Rejects_adjustment_that_makes_final_unit_price_negative()
    {
        var fluctuation = Valid(PriceFluctuationType.FixedAmount, -1_000_001);

        var error = Assert.Throws<InvalidOperationException>(() =>
            PriceFluctuationRules.CalculateAmount(1_000_000, fluctuation));

        Assert.Contains("nhỏ hơn 0", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_requires_non_zero_value_valid_period_and_reason()
    {
        var fluctuation = Valid(PriceFluctuationType.Percentage, 0);
        Assert.Throws<InvalidOperationException>(() => PriceFluctuationRules.EnsureCanSave(fluctuation));

        fluctuation.Value = -100.01m;
        Assert.Throws<InvalidOperationException>(() => PriceFluctuationRules.EnsureCanSave(fluctuation));

        fluctuation.Value = 10;
        fluctuation.EffectiveTo = fluctuation.EffectiveFrom.AddDays(-1);
        Assert.Throws<InvalidOperationException>(() => PriceFluctuationRules.EnsureCanSave(fluctuation));

        fluctuation.EffectiveTo = null;
        fluctuation.Reason = " ";
        Assert.Throws<InvalidOperationException>(() => PriceFluctuationRules.EnsureCanSave(fluctuation));
    }

    [Fact]
    public void Validation_enforces_percentage_and_money_precision()
    {
        var percentage = Valid(PriceFluctuationType.Percentage, 1.12345m);
        Assert.Throws<InvalidOperationException>(() => PriceFluctuationRules.EnsureCanSave(percentage));

        var fixedAmount = Valid(PriceFluctuationType.FixedAmount, 100.123m);
        Assert.Throws<InvalidOperationException>(() => PriceFluctuationRules.EnsureCanSave(fixedAmount));

        fixedAmount.Value = MoneyRules.MaxAmount + 1;
        Assert.Throws<InvalidOperationException>(() => PriceFluctuationRules.EnsureCanSave(fixedAmount));
    }

    [Fact]
    public void Only_locked_price_list_allows_fluctuation_management()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PriceFluctuationRules.EnsureCanManage(new PriceList { IsLocked = false }));

        PriceFluctuationRules.EnsureCanManage(new PriceList { IsLocked = true });
    }

    [Theory]
    [InlineData("2026-09-01", "2026-09-12", "2026-09-12", null, true)]
    [InlineData("2026-09-01", "2026-09-11", "2026-09-12", null, false)]
    [InlineData("2026-09-12", null, "2026-09-01", "2026-09-11", false)]
    public void Period_overlap_uses_inclusive_boundaries(
        string firstFrom,
        string? firstTo,
        string secondFrom,
        string? secondTo,
        bool expected)
    {
        Assert.Equal(expected, PriceFluctuationRules.PeriodsOverlap(
            DateTime.Parse(firstFrom),
            firstTo is null ? null : DateTime.Parse(firstTo),
            DateTime.Parse(secondFrom),
            secondTo is null ? null : DateTime.Parse(secondTo)));
    }

    [Fact]
    public void Pick_uses_only_adjustment_effective_on_quote_date()
    {
        var expired = WithPeriod(
            Valid(PriceFluctuationType.Percentage, 5), AsOf.AddDays(-10), AsOf.AddDays(-1));
        var current = WithPeriod(
            Valid(PriceFluctuationType.FixedAmount, -100_000), AsOf, AsOf.AddDays(10));

        var hit = PriceFluctuationRules.Pick([expired, current], AsOf);

        Assert.Same(current, hit);
    }

    [Fact]
    public void Quote_contains_adjusted_price_and_source_snapshot()
    {
        var list = new PriceList { Id = 4, Code = "BG-01", IsLocked = true };
        var item = new PriceListItem
        {
            Id = 8,
            UnitPrice = 1_000_000,
            Surcharge = 50_000,
            PriceListRevision = new PriceListRevision { PriceList = list },
        };
        var fluctuation = Valid(PriceFluctuationType.Percentage, 10);
        fluctuation.Id = 12;

        var quote = PriceListMatchRules.ToQuote(item, fluctuation);

        Assert.Equal(1_000_000, quote.BaseUnitPrice);
        Assert.Equal(100_000, quote.FluctuationAmount);
        Assert.Equal(1_100_000, quote.UnitPrice);
        Assert.Equal(50_000, quote.Surcharge);
        Assert.Equal(12, quote.PriceListFluctuationId);
        Assert.Contains("+10%", quote.SourceLabel, StringComparison.Ordinal);
        Assert.Contains("cuối 1,100,000", quote.SourceLabel, StringComparison.Ordinal);
    }

    private static PriceListFluctuation Valid(PriceFluctuationType type, decimal value) => new()
    {
        PriceListId = 1,
        Type = type,
        Value = value,
        EffectiveFrom = AsOf,
        Reason = "Giá dầu thay đổi",
    };

    private static PriceListFluctuation WithPeriod(
        PriceListFluctuation fluctuation,
        DateTime from,
        DateTime? to)
    {
        fluctuation.EffectiveFrom = from;
        fluctuation.EffectiveTo = to;
        return fluctuation;
    }
}
