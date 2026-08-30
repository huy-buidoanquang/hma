using Hma.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hma.Infrastructure.SqlServer;

public static class DependencyInjection
{
    public static IServiceCollection AddHmaInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<HmaDbContext>(o => o.UseSqlServer(connectionString));
        services.AddScoped<IHmaDbContext>(sp => sp.GetRequiredService<HmaDbContext>());
        services.AddScoped<IFileStorage, LocalDiskFileStorage>();
        return services;
    }
}
