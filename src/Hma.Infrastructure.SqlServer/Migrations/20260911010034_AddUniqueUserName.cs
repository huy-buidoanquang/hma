using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hma.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueUserName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT UserName
                    FROM dbo.AppUser
                    GROUP BY UserName
                    HAVING COUNT(*) > 1)
                    THROW 51005, N'Không thể áp dụng migration: tên đăng nhập bị trùng.', 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AppUser_UserName",
                table: "AppUser",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppUser_UserName",
                table: "AppUser");
        }
    }
}
