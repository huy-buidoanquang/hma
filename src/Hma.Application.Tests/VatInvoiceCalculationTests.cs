using Hma.Application.Features.Accounting;

namespace Hma.Application.Tests;

public class VatInvoiceCalculationTests
{
    [Theory]
    [InlineData(1_100_000, 10, 1_000_000, 100_000)]
    [InlineData(1_000_000, 0, 1_000_000, 0)]
    public void From_total_preserves_existing_invoice_formula(
        decimal total,
        decimal rate,
        decimal expectedAmount,
        decimal expectedVat)
    {
        var result = VatInvoiceCalculation.FromTotal(total, rate);

        Assert.Equal(expectedAmount, result.Amount);
        Assert.Equal(expectedVat, result.VatAmount);
    }

    [Fact]
    public void Invalid_negative_rate_preserves_existing_values_like_domain_entity()
    {
        var result = VatInvoiceCalculation.FromTotal(1_000, -100, 900, 100);

        Assert.Equal(900, result.Amount);
        Assert.Equal(100, result.VatAmount);
    }
}
