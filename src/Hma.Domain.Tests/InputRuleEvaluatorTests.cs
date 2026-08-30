using Hma.Domain.Services;

namespace Hma.Domain.Tests;

public class InputRuleEvaluatorTests
{
    [Fact]
    public void Required_empty_returns_message() =>
        Assert.Equal("Bắt buộc.", InputRuleEvaluator.Evaluate(InputRuleKind.Required, "  "));

    [Fact]
    public void Phone_empty_is_ok() =>
        Assert.Null(InputRuleEvaluator.Evaluate(InputRuleKind.Phone, ""));

    [Fact]
    public void Phone_invalid_returns_message() =>
        Assert.Contains("SĐT", InputRuleEvaluator.Evaluate(InputRuleKind.Phone, "que")!, StringComparison.Ordinal);

    [Fact]
    public void Email_invalid_returns_message() =>
        Assert.Contains("Email", InputRuleEvaluator.Evaluate(InputRuleKind.Email, "a@b")!, StringComparison.Ordinal);

    [Fact]
    public void MoneyPositive_zero_returns_message() =>
        Assert.Contains("lớn hơn 0", InputRuleEvaluator.Evaluate(InputRuleKind.MoneyPositive, "0")!, StringComparison.Ordinal);

    [Fact]
    public void Valid_values_pass()
    {
        Assert.Null(InputRuleEvaluator.Evaluate(InputRuleKind.Required | InputRuleKind.Phone, "0901234567"));
        Assert.Null(InputRuleEvaluator.Evaluate(InputRuleKind.Email, "a@b.com"));
        Assert.Null(InputRuleEvaluator.Evaluate(InputRuleKind.Money, "1,234,567"));
    }
}
