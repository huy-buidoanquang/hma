using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hma.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceListFluctuations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PriceListFluctuationId",
                table: "DispatchOrder",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PriceListFluctuation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PriceListId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(20,4)", precision: 20, scale: 4, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "date", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceListFluctuation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceListFluctuation_AppUser_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PriceListFluctuation_PriceList_PriceListId",
                        column: x => x.PriceListId,
                        principalTable: "PriceList",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_PriceListFluctuationId",
                table: "DispatchOrder",
                column: "PriceListFluctuationId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListFluctuation_CreatedByUserId",
                table: "PriceListFluctuation",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListFluctuation_PriceListId_EffectiveFrom",
                table: "PriceListFluctuation",
                columns: new[] { "PriceListId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchOrder_PriceListFluctuation_PriceListFluctuationId",
                table: "DispatchOrder",
                column: "PriceListFluctuationId",
                principalTable: "PriceListFluctuation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DispatchOrder_PriceListFluctuation_PriceListFluctuationId",
                table: "DispatchOrder");

            migrationBuilder.DropTable(
                name: "PriceListFluctuation");

            migrationBuilder.DropIndex(
                name: "IX_DispatchOrder_PriceListFluctuationId",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "PriceListFluctuationId",
                table: "DispatchOrder");
        }
    }
}
