using Hma.Application.Abstractions;
using Hma.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hma.Application.Services;

public class CompanyService(IHmaDbContext db, ICurrentUser current)
{
    public async Task<Company> GetAsync(CancellationToken ct = default) =>
        await db.Companies.FirstOrDefaultAsync(ct)
        ?? throw new InvalidOperationException("Chưa khai báo thông tin công ty.");

    public async Task SaveAsync(Company company, CancellationToken ct = default)
    {
        PermissionGuard.Require(current, ScreenKeys.Settings, PermissionAction.Update);
        if (string.IsNullOrWhiteSpace(company.Name))
            throw new InvalidOperationException("Tên công ty là bắt buộc.");
        company.Name = company.Name.Trim();
        company.Address = NullIfBlank(company.Address);
        company.Phone = NullIfBlank(company.Phone);
        company.TaxCode = NullIfBlank(company.TaxCode);
        company.Bank = NullIfBlank(company.Bank);
        company.Website = NullIfBlank(company.Website);
        company.Email = NullIfBlank(company.Email);

        if (company.Id == 0)
        {
            var existing = await db.Companies.FirstOrDefaultAsync(ct);
            if (existing is not null)
            {
                existing.Name = company.Name;
                existing.Address = company.Address;
                existing.Phone = company.Phone;
                existing.TaxCode = company.TaxCode;
                existing.Bank = company.Bank;
                existing.Website = company.Website;
                existing.Email = company.Email;
                db.Update(existing);
                await PersistenceGuard.SaveAsync(db, ct);
                company.Id = existing.Id;
                return;
            }
            db.Add(company);
        }
        else
            db.Update(company);
        await PersistenceGuard.SaveAsync(db, ct);
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
