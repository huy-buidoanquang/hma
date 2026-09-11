using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hma.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerCommercials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BuyExtraCost",
                table: "DispatchOrder",
                type: "decimal(20,2)",
                precision: 20,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BuyOverrideReason",
                table: "DispatchOrder",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyRateSourceSnapshot",
                table: "DispatchOrder",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BuySurcharge",
                table: "DispatchOrder",
                type: "decimal(20,2)",
                precision: 20,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BuyTotal",
                table: "DispatchOrder",
                type: "decimal(20,2)",
                precision: 20,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BuyUnitPrice",
                table: "DispatchOrder",
                type: "decimal(20,2)",
                precision: 20,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossMargin",
                table: "DispatchOrder",
                type: "decimal(20,2)",
                precision: 20,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsBuyManual",
                table: "DispatchOrder",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PartnerId",
                table: "DispatchOrder",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartnerNameSnapshot",
                table: "DispatchOrder",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PartnerOperatingFeePercent",
                table: "DispatchOrder",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PartnerPayableAmount",
                table: "DispatchOrder",
                type: "decimal(20,2)",
                precision: 20,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PartnerRateId",
                table: "DispatchOrder",
                type: "int",
                nullable: true);

            // Preserve the commercial meaning of the legacy partner report. Existing trips did
            // not store a buy quote, so their sell total was also used as the partner gross amount.
            // The UNASSIGNED partner is intentionally treated as no carrier assignment.
            migrationBuilder.Sql("""
                UPDATE d
                SET PartnerId = p.Id,
                    PartnerNameSnapshot = p.Name,
                    BuyUnitPrice = d.UnitPrice,
                    BuySurcharge = d.Surcharge,
                    BuyExtraCost = d.ExtraCost,
                    BuyTotal = d.TotalAmount,
                    PartnerOperatingFeePercent = p.OperatingFeePercent,
                    PartnerPayableAmount = ROUND(d.TotalAmount * (1 - p.OperatingFeePercent / 100.0), 2),
                    GrossMargin = d.TotalAmount - ROUND(d.TotalAmount * (1 - p.OperatingFeePercent / 100.0), 2),
                    IsBuyManual = 1,
                    BuyOverrideReason = N'Dữ liệu chuyển đổi: giá mua kế thừa từ cước bán trước khi bổ sung bảng giá đối tác.'
                FROM DispatchOrder d
                INNER JOIN Vehicle v ON v.Id = d.VehicleId
                INNER JOIN Partner p ON p.Id = v.PartnerId
                WHERE p.Code <> N'UNASSIGNED';

                UPDATE d
                SET GrossMargin = d.TotalAmount
                FROM DispatchOrder d
                WHERE d.PartnerId IS NULL;
                """);

            migrationBuilder.CreateTable(
                name: "PartnerRate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    RouteId = table.Column<int>(type: "int", nullable: false),
                    VehicleTypeId = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    Surcharge = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerRate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartnerRate_AppUser_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartnerRate_Partner_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partner",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartnerRate_Route_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Route",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartnerRate_VehicleType_VehicleTypeId",
                        column: x => x.VehicleTypeId,
                        principalTable: "VehicleType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartnerSettlement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedByUserId = table.Column<int>(type: "int", nullable: true),
                    FinalizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalizedByUserId = table.Column<int>(type: "int", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidedByUserId = table.Column<int>(type: "int", nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TripCount = table.Column<int>(type: "int", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    OperatingFeeAmount = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    PayableAmount = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerSettlement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartnerSettlement_AppUser_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartnerSettlement_AppUser_FinalizedByUserId",
                        column: x => x.FinalizedByUserId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartnerSettlement_AppUser_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartnerSettlement_AppUser_VoidedByUserId",
                        column: x => x.VoidedByUserId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartnerSettlement_Partner_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partner",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartnerSettlementLine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerSettlementId = table.Column<int>(type: "int", nullable: false),
                    DispatchOrderId = table.Column<int>(type: "int", nullable: false),
                    TripDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DispatchCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Route = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PlateNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DriverName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    BuyTotal = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    OperatingFeePercent = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    OperatingFeeAmount = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    PayableAmount = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerSettlementLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartnerSettlementLine_DispatchOrder_DispatchOrderId",
                        column: x => x.DispatchOrderId,
                        principalTable: "DispatchOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartnerSettlementLine_PartnerSettlement_PartnerSettlementId",
                        column: x => x.PartnerSettlementId,
                        principalTable: "PartnerSettlement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_PartnerId",
                table: "DispatchOrder",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_PartnerRateId",
                table: "DispatchOrder",
                column: "PartnerRateId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerRate_CreatedByUserId",
                table: "PartnerRate",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerRate_PartnerId_RouteId_VehicleTypeId_EffectiveFrom",
                table: "PartnerRate",
                columns: new[] { "PartnerId", "RouteId", "VehicleTypeId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartnerRate_RouteId",
                table: "PartnerRate",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerRate_VehicleTypeId",
                table: "PartnerRate",
                column: "VehicleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerSettlement_CreatedByUserId",
                table: "PartnerSettlement",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerSettlement_FinalizedByUserId",
                table: "PartnerSettlement",
                column: "FinalizedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerSettlement_PartnerId_Year_Month",
                table: "PartnerSettlement",
                columns: new[] { "PartnerId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartnerSettlement_SubmittedByUserId",
                table: "PartnerSettlement",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerSettlement_VoidedByUserId",
                table: "PartnerSettlement",
                column: "VoidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerSettlementLine_DispatchOrderId",
                table: "PartnerSettlementLine",
                column: "DispatchOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartnerSettlementLine_PartnerSettlementId",
                table: "PartnerSettlementLine",
                column: "PartnerSettlementId");

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchOrder_PartnerRate_PartnerRateId",
                table: "DispatchOrder",
                column: "PartnerRateId",
                principalTable: "PartnerRate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchOrder_Partner_PartnerId",
                table: "DispatchOrder",
                column: "PartnerId",
                principalTable: "Partner",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DispatchOrder_PartnerRate_PartnerRateId",
                table: "DispatchOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchOrder_Partner_PartnerId",
                table: "DispatchOrder");

            migrationBuilder.DropTable(
                name: "PartnerRate");

            migrationBuilder.DropTable(
                name: "PartnerSettlementLine");

            migrationBuilder.DropTable(
                name: "PartnerSettlement");

            migrationBuilder.DropIndex(
                name: "IX_DispatchOrder_PartnerId",
                table: "DispatchOrder");

            migrationBuilder.DropIndex(
                name: "IX_DispatchOrder_PartnerRateId",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "BuyExtraCost",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "BuyOverrideReason",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "BuyRateSourceSnapshot",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "BuySurcharge",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "BuyTotal",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "BuyUnitPrice",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "GrossMargin",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "IsBuyManual",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "PartnerId",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "PartnerNameSnapshot",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "PartnerOperatingFeePercent",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "PartnerPayableAmount",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "PartnerRateId",
                table: "DispatchOrder");
        }
    }
}
