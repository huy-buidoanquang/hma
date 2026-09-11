using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hma.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddFreightPriceTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FreightOverrideReason",
                table: "DispatchOrder",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFreightManual",
                table: "DispatchOrder",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PriceListItemId",
                table: "DispatchOrder",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceSourceSnapshot",
                table: "DispatchOrder",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE dbo.DispatchOrder
                SET IsFreightManual = 1,
                    FreightOverrideReason = N'Dữ liệu có trước chức năng lưu nguồn bảng giá.';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_PriceListItemId",
                table: "DispatchOrder",
                column: "PriceListItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchOrder_PriceListItem_PriceListItemId",
                table: "DispatchOrder",
                column: "PriceListItemId",
                principalTable: "PriceListItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DispatchOrder_PriceListItem_PriceListItemId",
                table: "DispatchOrder");

            migrationBuilder.DropIndex(
                name: "IX_DispatchOrder_PriceListItemId",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "FreightOverrideReason",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "IsFreightManual",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "PriceListItemId",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "PriceSourceSnapshot",
                table: "DispatchOrder");
        }
    }
}
