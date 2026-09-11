using Hma.Domain.Entities;
using Hma.Domain.Services;

namespace Hma.Application.Services;

public static class AmountText
{
    public static string From(decimal amount) => VietnameseAmountWords.ToWords(amount);

    public static void Refresh(DispatchOrder order) => order.AmountInWords = From(order.TotalAmount);
}
