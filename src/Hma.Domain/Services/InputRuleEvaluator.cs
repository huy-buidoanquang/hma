namespace Hma.Domain.Services;

public static class InputRuleEvaluator
{
    public static string? Evaluate(InputRuleKind kind, string? text)
    {
        var value = text?.Trim();
        if (kind.HasFlag(InputRuleKind.Required) && string.IsNullOrWhiteSpace(value))
            return "Bắt buộc.";

        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (kind.HasFlag(InputRuleKind.Phone) && !PhoneRules.IsValid(value))
            return "SĐT không hợp lệ (0xxx hoặc +84).";
        if (kind.HasFlag(InputRuleKind.Email) && !EmailRules.IsValid(value))
            return "Email không hợp lệ.";
        if (kind.HasFlag(InputRuleKind.TaxCode) && !TaxCodeRules.IsValid(value))
            return "MST không hợp lệ (10 hoặc 13 số).";

        if (kind.HasFlag(InputRuleKind.Money) || kind.HasFlag(InputRuleKind.MoneyPositive))
        {
            if (!MoneyRules.TryParse(value, out var amount))
                return "Số tiền không hợp lệ.";
            try
            {
                if (kind.HasFlag(InputRuleKind.MoneyPositive))
                    MoneyRules.EnsurePositive(amount, "Số tiền");
                else
                    MoneyRules.EnsureNonNegative(amount, "Số tiền");
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }
        }

        return null;
    }
}
