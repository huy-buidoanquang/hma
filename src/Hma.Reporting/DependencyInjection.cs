using Hma.Application.Abstractions.Import;
using Hma.Application.Abstractions.Reporting;
using Hma.Reporting;
using Microsoft.Extensions.DependencyInjection;

namespace Hma.Reporting;

public static class DependencyInjection
{
    public static IServiceCollection AddHmaReporting(this IServiceCollection services)
    {
        services.AddSingleton<DocumentPrinter>();
        services.AddSingleton<IDocumentRenderer, ReportDocumentRenderer>();
        services.AddSingleton<IDispatchImportParser, DispatchImportParser>();
        return services;
    }
}
