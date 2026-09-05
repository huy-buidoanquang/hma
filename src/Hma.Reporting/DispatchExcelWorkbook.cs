using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace Hma.Reporting;

internal static class DispatchExcelWorkbook
{
    private static readonly Regex PrintBreaks = new(
        @"<(row|col)Breaks\b[^>]*>[\s\S]*?</\1Breaks>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static XLWorkbook Open(Stream stream)
    {
        var copy = new MemoryStream();
        stream.CopyTo(copy);
        copy.Position = 0;
        StripPrintBreaks(copy);
        copy.Position = 0;
        return new XLWorkbook(copy);
    }

    private static void StripPrintBreaks(MemoryStream buffer)
    {
        using var zip = new ZipArchive(buffer, ZipArchiveMode.Update, leaveOpen: true);
        var names = zip.Entries
            .Select(e => e.FullName)
            .Where(n => n.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase)
                        && n.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var name in names)
        {
            var entry = zip.GetEntry(name);
            if (entry is null) continue;
            string xml;
            using (var reader = new StreamReader(entry.Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                xml = reader.ReadToEnd();
            var next = PrintBreaks.Replace(xml, "");
            if (next == xml) continue;
            entry.Delete();
            var created = zip.CreateEntry(name, CompressionLevel.Fastest);
            using var writer = new StreamWriter(created.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            writer.Write(next);
        }
    }
}
