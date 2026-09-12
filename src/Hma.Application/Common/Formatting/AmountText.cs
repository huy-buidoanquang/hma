using Hma.Domain.Formatting;
using Hma.Domain.Entities;
using Hma.Domain.Rules;

namespace Hma.Application.Common.Formatting;

public static class AmountText
{
    public static string From(decimal amount) => VietnameseAmountWords.ToWords(amount);

    public static void Refresh(DispatchOrder order) => order.AmountInWords = From(order.TotalAmount);
}
