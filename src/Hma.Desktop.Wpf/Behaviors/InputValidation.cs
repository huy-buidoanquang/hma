using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Hma.Domain.Services;

namespace Hma.Desktop.Wpf.Behaviors;

public static class InputValidation
{
    public static readonly DependencyProperty RuleProperty =
        DependencyProperty.RegisterAttached(
            "Rule",
            typeof(InputRuleKind),
            typeof(InputValidation),
            new PropertyMetadata(InputRuleKind.None, OnRuleChanged));

    private static readonly DependencyProperty VisitedProperty =
        DependencyProperty.RegisterAttached(
            "Visited",
            typeof(bool),
            typeof(InputValidation),
            new PropertyMetadata(false));

    public static void SetRule(DependencyObject element, InputRuleKind value) =>
        element.SetValue(RuleProperty, value);

    public static InputRuleKind GetRule(DependencyObject element) =>
        (InputRuleKind)element.GetValue(RuleProperty);

    private static void OnRuleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox box) return;
        box.TextChanged -= OnTextChanged;
        box.LostFocus -= OnLostFocus;
        if ((InputRuleKind)e.NewValue != InputRuleKind.None)
        {
            box.TextChanged += OnTextChanged;
            box.LostFocus += OnLostFocus;
        }
    }

    private static void OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox box) return;
        box.SetValue(VisitedProperty, true);
        Apply(box);
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox box) return;
        if (Equals(box.GetValue(VisitedProperty), true) || !string.IsNullOrWhiteSpace(box.Text))
            Apply(box);
    }

    private static void Apply(TextBox box)
    {
        var expr = BindingOperations.GetBindingExpression(box, TextBox.TextProperty);
        if (expr is null) return;

        var error = InputRuleEvaluator.Evaluate(GetRule(box), box.Text);
        if (error is null)
        {
            Validation.ClearInvalid(expr);
            return;
        }

        Validation.MarkInvalid(expr, new ValidationError(new ExceptionValidationRule(), expr, error, null));
    }
}
