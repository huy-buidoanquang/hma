using Hma.Application.Features.Reconciliation;
using Hma.Application.Common.Security;
using Hma.Application.Features.PartnerSettlements;
using Hma.Application.Features.Dispatching;
using Hma.Application.Features.Dispatching.Import;
using Hma.Application.Features.Accounting;
using Hma.Application.Common.Persistence;
using Hma.Application.Features.Customers;
using Hma.Application.Features.TransportExceptions;
using Hma.Application.Features.Reporting;
using Hma.Application.Features.Catalogs;
using Hma.Application.Features.Routes;
using Hma.Application.Features.Statements;
using Hma.Application.Abstractions.Security;
using Hma.Application.Features.Pricing;
using Hma.Application.Features.Settings;
using Hma.Application.Features.Authentication;
using Hma.Application.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hma.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddHmaApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IDocumentNumberService, DocumentNumberService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IChangeLogService, ChangeLogService>();
        services.AddScoped<ChangeLogService>();
        services.AddScoped<UserAdminService>();
        services.AddScoped<CustomerService>();
        services.AddScoped<CatalogOptionQueryService>();
        services.AddScoped<CityService>();
        services.AddScoped<DepartmentService>();
        services.AddScoped<JobTitleService>();
        services.AddScoped<EmployeeService>();
        services.AddScoped<PartnerService>();
        services.AddScoped<DriverService>();
        services.AddScoped<VehicleService>();
        services.AddScoped<PriceListService>();
        services.AddScoped<PartnerRateService>();
        services.AddScoped<PartnerSettlementService>();
        services.AddScoped<TransportExceptionService>();
        services.AddScoped<DispatchOrderQueryService>();
        services.AddScoped<DispatchOrderEditorService>();
        services.AddScoped<DispatchReconciliationService>();
        services.AddScoped<DispatchGridService>();
        services.AddScoped<WalkInCustomerService>();
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
