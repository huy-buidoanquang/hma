using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hma.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddDestinationPriceFallback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PriceListItem_PriceListRevisionId_RouteId_VehicleTypeId",
                table: "PriceListItem");

            migrationBuilder.AlterColumn<int>(
                name: "RouteId",
                table: "PriceListItem",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "DeliveryLocationId",
                table: "PriceListItem",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItem_DeliveryLocationId",
                table: "PriceListItem",
                column: "DeliveryLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItem_PriceListRevisionId_DeliveryLocationId_VehicleTypeId",
                table: "PriceListItem",
                columns: new[] { "PriceListRevisionId", "DeliveryLocationId", "VehicleTypeId" },
                unique: true,
                filter: "[RouteId] IS NULL AND [DeliveryLocationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItem_PriceListRevisionId_RouteId_VehicleTypeId",
                table: "PriceListItem",
                columns: new[] { "PriceListRevisionId", "RouteId", "VehicleTypeId" },
                unique: true,
                filter: "[RouteId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_PriceListItem_Location_DeliveryLocationId",
                table: "PriceListItem",
                column: "DeliveryLocationId",
                principalTable: "Location",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PriceListItem_Location_DeliveryLocationId",
                table: "PriceListItem");

            migrationBuilder.DropIndex(
                name: "IX_PriceListItem_DeliveryLocationId",
                table: "PriceListItem");

            migrationBuilder.DropIndex(
                name: "IX_PriceListItem_PriceListRevisionId_DeliveryLocationId_VehicleTypeId",
                table: "PriceListItem");

            migrationBuilder.DropIndex(
                name: "IX_PriceListItem_PriceListRevisionId_RouteId_VehicleTypeId",
                table: "PriceListItem");

            migrationBuilder.DropColumn(
                name: "DeliveryLocationId",
                table: "PriceListItem");

            migrationBuilder.AlterColumn<int>(
                name: "RouteId",
                table: "PriceListItem",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItem_PriceListRevisionId_RouteId_VehicleTypeId",
                table: "PriceListItem",
                columns: new[] { "PriceListRevisionId", "RouteId", "VehicleTypeId" },
                unique: true);
        }
    }
}
