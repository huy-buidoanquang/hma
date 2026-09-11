using Hma.Application.Abstractions;
using Hma.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hma.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddHmaApplication(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ICurrentUser, CurrentUser>();
        services.AddScoped<IDocumentNumberService, DocumentNumberService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IChangeLogService, ChangeLogService>();
        services.AddScoped<ChangeLogService>();
        services.AddScoped<UserAdminService>();
        services.AddScoped<CustomerService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<PriceListService>();
        services.AddScoped<PartnerRateService>();
        services.AddScoped<PartnerSettlementService>();
        services.AddScoped<TransportExceptionService>();
        services.AddScoped<DispatchOrderService>();
        services.AddScoped<DispatchImportService>();
        services.AddScoped<DispatchDocumentService>();
        services.AddScoped<FreightStatementService>();
        services.AddScoped<DashboardQueryService>();
        services.AddScoped<CashDocumentService>();
        services.AddScoped<VatInvoiceService>();
        services.AddScoped<ReportQueryService>();
        services.AddScoped<SettingsService>();
        services.AddScoped<SystemHealthService>();
        services.AddScoped<CompanyService>();
        services.AddScoped<LocationService>();
        services.AddScoped<RouteService>();
        services.AddScoped<LocationAliasService>();
        services.AddScoped<RouteAliasService>();
        services.AddScoped<CustomerAliasService>();
        return services;
    }
}
