using System.IO;
using System.Text.Json;
using System.Windows;

namespace Hma.Desktop.Wpf.Theming;

public sealed class ThemeService
{
    public const string Light = "Light";
    public const string Dark = "Dark";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public IReadOnlyList<ThemeOption> Options { get; } =
    [
        new(Light, "Sáng"),
        new(Dark, "Tối")
    ];

    public string Current { get; private set; } = Light;

    public void LoadAndApply()
    {
        var theme = Light;
        try
        {
            var path = PreferencesPath();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var prefs = JsonSerializer.Deserialize<UiPreferences>(json);
                if (prefs?.Theme is not null)
                    theme = prefs.Theme;
            }
        }
        catch (IOException)
        {
        }
        catch (JsonException)
        {
        }

        Apply(theme, persist: false);
    }

    public void Apply(string theme, bool persist = true)
    {
        var name = theme.Equals(Dark, StringComparison.OrdinalIgnoreCase) ? Dark : Light;
        var uri = name == Dark
            ? new Uri("pack://application:,,,/Themes/Colors.Dark.xaml", UriKind.Absolute)
            : new Uri("pack://application:,,,/Themes/Colors.Light.xaml", UriKind.Absolute);

        var dicts = System.Windows.Application.Current.Resources.MergedDictionaries;
        var next = new ResourceDictionary { Source = uri };
        var existing = dicts.FirstOrDefault(IsThemeDictionary);
        var index = existing is null ? 1 : dicts.IndexOf(existing);
        if (existing is not null)
            dicts.Remove(existing);
        dicts.Insert(Math.Min(index, dicts.Count), next);

        Current = name;
        if (persist)
            Write(name);
    }

    private static bool IsThemeDictionary(ResourceDictionary dictionary)
    {
        var source = dictionary.Source?.OriginalString ?? "";
        return source.Contains("Colors.Light.xaml", StringComparison.OrdinalIgnoreCase)
               || source.Contains("Colors.Dark.xaml", StringComparison.OrdinalIgnoreCase);
    }

    private static void Write(string theme)
    {
        try
        {
            var path = PreferencesPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(new UiPreferences { Theme = theme }, JsonOptions));
        }
        catch (IOException)
        {
        }
    }

    private static string PreferencesPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hma", "ui.json");
}
