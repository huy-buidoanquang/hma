namespace Hma.Desktop.Wpf.Presentation.Common.Validation;

[Flags]
public enum InputRuleKind
{
    None = 0,
    Required = 1,
    Phone = 2,
    Email = 4,
    TaxCode = 8,
    Money = 16,
    MoneyPositive = 32
}
