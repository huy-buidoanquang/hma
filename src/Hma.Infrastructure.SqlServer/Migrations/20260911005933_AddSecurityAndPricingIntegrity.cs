using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hma.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityAndPricingIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT PriceListRevisionId, RouteId, VehicleTypeId
                    FROM dbo.PriceListItem
                    GROUP BY PriceListRevisionId, RouteId, VehicleTypeId
                    HAVING COUNT(*) > 1)
                    THROW 51002, N'Không thể áp dụng migration: phiên bản bảng giá có dòng trùng tuyến và loại xe.', 1;

                IF EXISTS (
                    SELECT Code
                    FROM dbo.DispatchOrder
                    GROUP BY Code
                    HAVING COUNT(*) > 1)
                    THROW 51003, N'Không thể áp dụng migration: số lệnh điều xe bị trùng.', 1;

                IF EXISTS (SELECT 1 FROM dbo.AppUser WHERE LEN(UserName) > 50 OR LEN(PasswordHash) > 255 OR LEN(DisplayName) > 255)
                    THROW 51004, N'Không thể áp dụng migration: dữ liệu người dùng vượt quá độ dài cho phép.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_PriceListItem_PriceListRevisionId",
                table: "PriceListItem");

            migrationBuilder.DropIndex(
                name: "IX_DispatchOrder_Code",
                table: "DispatchOrder");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "AppUser",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "AppUser",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "AppUser",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                table: "AppUser",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "AppUser",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEnd",
                table: "AppUser",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItem_PriceListRevisionId_RouteId_VehicleTypeId",
                table: "PriceListItem",
                columns: new[] { "PriceListRevisionId", "RouteId", "VehicleTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_Code",
                table: "DispatchOrder",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PriceListItem_PriceListRevisionId_RouteId_VehicleTypeId",
                table: "PriceListItem");

            migrationBuilder.DropIndex(
                name: "IX_DispatchOrder_Code",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                table: "AppUser");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "AppUser");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "AppUser");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "AppUser",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "AppUser",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "AppUser",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItem_PriceListRevisionId",
                table: "PriceListItem",
                column: "PriceListRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_Code",
                table: "DispatchOrder",
                column: "Code");
        }
    }
}
