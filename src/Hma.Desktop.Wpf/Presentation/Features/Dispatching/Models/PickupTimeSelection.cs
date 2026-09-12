namespace Hma.Desktop.Wpf.Presentation.Features.Dispatching.Models;

public readonly record struct PickupTimeSelection(DateTime Date, int Hour, int Minute)
{
    public static PickupTimeSelection From(DateTime value) =>
        new(value.Date, value.Hour, value.Minute);

    public DateTime ToDateTime() => Date.Date.AddHours(Hour).AddMinutes(Minute);
}
