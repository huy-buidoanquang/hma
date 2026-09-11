namespace Hma.Reporting;

public static class TemporaryReportFile
{
    public static string Create(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(stem) || string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("Tên tệp báo cáo phải có phần tên và phần mở rộng.", nameof(fileName));

        return Path.Combine(
            Path.GetTempPath(),
            $"{stem}-{DateTime.Now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}{extension}");
    }
}
