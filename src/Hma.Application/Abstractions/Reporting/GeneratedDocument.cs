namespace Hma.Application.Abstractions.Reporting;

public sealed record GeneratedDocument(string FileName, string ContentType, byte[] Content);
