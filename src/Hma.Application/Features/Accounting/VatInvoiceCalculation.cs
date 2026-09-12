namespace Hma.Application.Features.Accounting;

public static class VatInvoiceCalculation
{
    public static VatInvoiceAmounts FromTotal(
        decimal totalAmount,
        decimal vatRate,
        decimal currentAmount = 0,
        decimal currentVatAmount = 0)
    {
        if (vatRate <= -100)
            return new VatInvoiceAmounts(currentAmount, currentVatAmount);
        var amount = Math.Round(totalAmount / (1 + vatRate / 100m), 0);
        return new VatInvoiceAmounts(amount, totalAmount - amount);
    }
}
