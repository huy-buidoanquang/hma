using System.Globalization;
using System.Text;

namespace Hma.Desktop.Wpf.Presentation.Common.Editing;

public readonly record struct EditorState(string Value)
{
    public static EditorState Capture(params object?[] values)
    {
        var builder = new StringBuilder();
        foreach (var value in values)
        {
            var text = value switch
            {
                null => "",
                DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
                DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? ""
            };
            builder.Append(value?.GetType().FullName ?? "null")
                .Append(':')
                .Append(text.Length)
                .Append(':')
                .Append(text);
        }
        return new EditorState(builder.ToString());
    }
}
