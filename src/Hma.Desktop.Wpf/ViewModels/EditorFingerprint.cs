using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Hma.Desktop.Wpf.ViewModels;

/// <summary>
/// So sánh giá trị scalar của editor (bỏ navigation) để biết form có bị sửa không.
/// </summary>
internal static class EditorFingerprint
{
    public static string Of(params object?[] parts)
    {
        var builder = new StringBuilder();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < parts.Length; i++)
            Write(parts[i], i.ToString(CultureInfo.InvariantCulture), builder, seen);
        return builder.ToString();
    }

    private static void Write(object? value, string path, StringBuilder builder, HashSet<object> seen)
    {
        if (value is null)
        {
            builder.Append(path).Append("=\n");
            return;
        }

        if (value is string text)
        {
            builder.Append(path).Append('=').Append(text).Append('\n');
            return;
        }

        if (value is byte[])
            return;

        var type = value.GetType();
        if (type.IsPrimitive || value is decimal or DateTime or DateTimeOffset or TimeSpan or Guid || type.IsEnum)
        {
            builder.Append(path).Append('=').Append(Convert.ToString(value, CultureInfo.InvariantCulture)).Append('\n');
            return;
        }

        if (value is IEnumerable enumerable)
        {
            var n = 0;
            foreach (var item in enumerable)
                Write(item, path + "[" + n++ + "]", builder, seen);
            builder.Append(path).Append(".Count=").Append(n).Append('\n');
            return;
        }

        if (!seen.Add(value))
            return;

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetIndexParameters().Length > 0 || prop.SetMethod is null)
                continue;
            if (prop.Name is "RowVersion" or "PasswordHash")
                continue;
            var propertyType = prop.PropertyType;
            if (propertyType.IsClass && propertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(propertyType))
                continue;
            if (propertyType.IsClass && propertyType != typeof(string) && !propertyType.IsArray)
                continue;

            object? propertyValue;
            try
            {
                propertyValue = prop.GetValue(value);
            }
            catch (Exception)
            {
                continue;
            }

            Write(propertyValue, path + "." + prop.Name, builder, seen);
        }
    }
}
