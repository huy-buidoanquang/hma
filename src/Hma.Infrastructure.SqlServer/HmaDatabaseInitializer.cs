using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Hma.Infrastructure.SqlServer;

public sealed class HmaDatabaseInitializer(HmaDbContext db)
{
    public async Task MigrateAndSeedAsync(CancellationToken cancellationToken = default)
    {
        await BaselineEnsureCreatedDatabaseAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
        await DatabaseSeeder.SeedAsync(db, cancellationToken);
    }

    /// <summary>
    /// DB tạo bằng EnsureCreated không có __EFMigrationsHistory.
    /// Bắt nhịp cột còn thiếu rồi ghi InitialCreate là đã áp dụng — không DROP, không tạo lại bảng.
    /// Migration sau InitialCreate vẫn chạy bình thường qua MigrateAsync.
    /// </summary>
    private async Task BaselineEnsureCreatedDatabaseAsync(CancellationToken cancellationToken)
    {
        if (!await db.Database.CanConnectAsync(cancellationToken))
            return;
        if (!await ObjectExistsAsync("Partner", cancellationToken))
            return;

        var history = db.GetService<IHistoryRepository>();
        if (await history.ExistsAsync(cancellationToken))
            return;

        await CatchUpEnsureCreatedSchemaAsync(cancellationToken);

        var create = history.GetCreateIfNotExistsScript();
        await db.Database.ExecuteSqlRawAsync(create, cancellationToken);

        var initial = db.Database.GetMigrations().FirstOrDefault();
        if (initial is null)
            return;

        var productVersion = ProductInfo.GetVersion();
        var insert = history.GetInsertScript(new HistoryRow(initial, productVersion));
        await db.Database.ExecuteSqlRawAsync(insert, cancellationToken);
    }

    private async Task CatchUpEnsureCreatedSchemaAsync(CancellationToken cancellationToken)
    {
        await ExecuteSqlAsync("""
            IF OBJECT_ID(N'dbo.PaymentMethod', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.PaymentMethod (
                    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PaymentMethod PRIMARY KEY,
                    Code NVARCHAR(MAX) NOT NULL,
                    Name NVARCHAR(MAX) NOT NULL,
                    LegacyId INT NULL
                );
            END
            """, cancellationToken);

        await EnsureColumnAsync("Partner", "OperatingFeePercent",
            "DECIMAL(9,2) NOT NULL CONSTRAINT DF_Partner_OpFee DEFAULT (0)", cancellationToken);
        await EnsureColumnAsync("DispatchOrder", "PaymentMethodId", "INT NULL", cancellationToken);
        await EnsureColumnAsync("DispatchOrder", "BillingYear",
            "INT NOT NULL CONSTRAINT DF_DispatchOrder_BillYear DEFAULT (0)", cancellationToken);
        await EnsureColumnAsync("DispatchOrder", "BillingMonth",
            "INT NOT NULL CONSTRAINT DF_DispatchOrder_BillMonth DEFAULT (0)", cancellationToken);
        await EnsureColumnAsync("DispatchOrder", "ConfirmedByUserId", "INT NULL", cancellationToken);
        await EnsureColumnAsync("DispatchOrder", "ConfirmedAt", "DATETIME2 NULL", cancellationToken);
        await EnsureColumnAsync("DispatchOrder", "ArNumber", "NVARCHAR(50) NULL", cancellationToken);
        await ExecuteSqlAsync(
            "UPDATE dbo.DispatchOrder SET BillingYear = YEAR(PickupAt), BillingMonth = MONTH(PickupAt) WHERE BillingYear = 0 OR BillingMonth = 0",
            cancellationToken);
    }

    private async Task EnsureColumnAsync(string table, string column, string sqlType, CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync(table, column, cancellationToken))
            return;
        await ExecuteSqlAsync($"ALTER TABLE dbo.[{table}] ADD [{column}] {sqlType}", cancellationToken);
    }

    private async Task<bool> ObjectExistsAsync(string table, CancellationToken cancellationToken)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await db.Database.OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT CASE WHEN OBJECT_ID(N'dbo.{table}', N'U') IS NULL THEN 0 ELSE 1 END";
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) == 1;
    }

    private async Task<bool> ColumnExistsAsync(string table, string column, CancellationToken cancellationToken)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await db.Database.OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT CASE WHEN COL_LENGTH(N'dbo.{table}', N'{column}') IS NULL THEN 0 ELSE 1 END";
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) == 1;
    }

    private async Task ExecuteSqlAsync(string sql, CancellationToken cancellationToken)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await db.Database.OpenConnectionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
