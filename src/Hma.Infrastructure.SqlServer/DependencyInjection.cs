using Hma.Application.Abstractions.Persistence;
using Hma.Application.Abstractions.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hma.Infrastructure.SqlServer;

public static class DependencyInjection
{
    public static IServiceCollection AddHmaInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<HmaDbContext>(o => o.UseSqlServer(connectionString, sql =>
            sql.MigrationsAssembly(typeof(HmaDbContext).Assembly.GetName().Name)));
        services.AddScoped<IHmaDbContext>(sp => sp.GetRequiredService<HmaDbContext>());
        services.AddScoped<HmaDatabaseInitializer>();
        services.AddScoped<IFileStorage, LocalDiskFileStorage>();
        return services;
    }
}
