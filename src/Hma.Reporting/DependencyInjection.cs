using Hma.Application.Abstractions;
using Hma.Reporting;
using Microsoft.Extensions.DependencyInjection;

namespace Hma.Reporting;

public static class DependencyInjection
{
    public static IServiceCollection AddHmaReporting(this IServiceCollection services)
    {
        services.AddSingleton<IDocumentPrinter, DocumentPrinter>();
        services.AddSingleton<IDispatchImportParser, DispatchImportParser>();
        return services;
    }
}
