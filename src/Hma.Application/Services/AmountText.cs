using Hma.Domain.Services;

namespace Hma.Application.Services;

public static class AmountText
{
    public static string From(decimal amount) => VietnameseAmountWords.ToWords(amount);
}
