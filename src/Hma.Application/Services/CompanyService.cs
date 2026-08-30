using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class CompanyService(IHmaDbContext db)
{
    public async Task<Company> GetAsync(CancellationToken ct = default) =>
        await db.Companies.FirstOrDefaultAsync(ct)
        ?? throw new InvalidOperationException("Chưa khai báo thông tin công ty.");
}
