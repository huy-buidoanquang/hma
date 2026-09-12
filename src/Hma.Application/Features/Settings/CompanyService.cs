using Hma.Application.Abstractions.Persistence;
using Hma.Application.Common.Persistence;
using Hma.Application.Abstractions.Security;
using Hma.Application.Common.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Features.Settings;

public class CompanyService(IHmaDbContext db, ICurrentUser current)
{
    public async Task<CompanyDetails> GetAsync(CancellationToken ct = default)
    {
        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Chưa khai báo thông tin công ty.");
        return ToDetails(company);
    }

    public async Task<CompanyDetails> SaveAsync(SaveCompanyCommand command, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.Settings, PermissionAction.Update);
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new InvalidOperationException("Tên công ty là bắt buộc.");
        var company = command.Id == 0
            ? await db.Companies.FirstOrDefaultAsync(ct)
            : await db.Companies.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (company is null)
        {
            company = new Hma.Domain.Entities.Company();
            db.Add(company);
        }
        company.Name = command.Name.Trim();
        company.Address = NullIfBlank(command.Address);
        company.Phone = NullIfBlank(command.Phone);
        company.TaxCode = NullIfBlank(command.TaxCode);
        company.Bank = NullIfBlank(command.Bank);
        company.Website = NullIfBlank(command.Website);
        company.Email = NullIfBlank(command.Email);
        await PersistenceGuard.SaveAsync(db, ct);
        return ToDetails(company);
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static CompanyDetails ToDetails(Hma.Domain.Entities.Company company) => new(
        company.Id, company.Name, company.Address, company.Phone, company.TaxCode,
        company.Bank, company.Website, company.Email);
}
