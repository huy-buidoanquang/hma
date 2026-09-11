using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hma.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddTransportExceptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedExceptionCost",
                table: "DispatchOrder",
                type: "decimal(20,2)",
                precision: 20,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedExceptionRevenue",
                table: "DispatchOrder",
                type: "decimal(20,2)",
                precision: 20,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "TransportExceptionCode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportExceptionCode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransportException",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DispatchOrderId = table.Column<int>(type: "int", nullable: false),
                    TransportExceptionCodeId = table.Column<int>(type: "int", nullable: false),
                    CodeSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameSnapshot = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CustomerCharge = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    PartnerCost = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidedByUserId = table.Column<int>(type: "int", nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportException", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransportException_AppUser_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransportException_AppUser_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransportException_AppUser_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransportException_AppUser_VoidedByUserId",
                        column: x => x.VoidedByUserId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransportException_DispatchOrder_DispatchOrderId",
                        column: x => x.DispatchOrderId,
                        principalTable: "DispatchOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransportException_TransportExceptionCode_TransportExceptionCodeId",
                        column: x => x.TransportExceptionCodeId,
                        principalTable: "TransportExceptionCode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransportException_CreatedByUserId",
                table: "TransportException",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportException_DispatchOrderId",
                table: "TransportException",
                column: "DispatchOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportException_ReviewedByUserId",
                table: "TransportException",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportException_SubmittedByUserId",
                table: "TransportException",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportException_TransportExceptionCodeId",
                table: "TransportException",
                column: "TransportExceptionCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportException_VoidedByUserId",
                table: "TransportException",
                column: "VoidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportExceptionCode_Code",
                table: "TransportExceptionCode",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransportException");

            migrationBuilder.DropTable(
                name: "TransportExceptionCode");

            migrationBuilder.DropColumn(
                name: "ApprovedExceptionCost",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "ApprovedExceptionRevenue",
                table: "DispatchOrder");
        }
    }
}
