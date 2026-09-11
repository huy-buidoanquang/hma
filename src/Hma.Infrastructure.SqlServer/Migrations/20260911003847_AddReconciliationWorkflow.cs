using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hma.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddReconciliationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReconciliationRejectedAt",
                table: "DispatchOrder",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReconciliationRejectedByUserId",
                table: "DispatchOrder",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReconciliationRejectionReason",
                table: "DispatchOrder",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReconciliationSubmittedAt",
                table: "DispatchOrder",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReconciliationSubmittedByUserId",
                table: "DispatchOrder",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_ReconciliationRejectedByUserId",
                table: "DispatchOrder",
                column: "ReconciliationRejectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_ReconciliationSubmittedByUserId",
                table: "DispatchOrder",
                column: "ReconciliationSubmittedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchOrder_AppUser_ReconciliationRejectedByUserId",
                table: "DispatchOrder",
                column: "ReconciliationRejectedByUserId",
                principalTable: "AppUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchOrder_AppUser_ReconciliationSubmittedByUserId",
                table: "DispatchOrder",
                column: "ReconciliationSubmittedByUserId",
                principalTable: "AppUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DispatchOrder_AppUser_ReconciliationRejectedByUserId",
                table: "DispatchOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchOrder_AppUser_ReconciliationSubmittedByUserId",
                table: "DispatchOrder");

            migrationBuilder.DropIndex(
                name: "IX_DispatchOrder_ReconciliationRejectedByUserId",
                table: "DispatchOrder");

            migrationBuilder.DropIndex(
                name: "IX_DispatchOrder_ReconciliationSubmittedByUserId",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "ReconciliationRejectedAt",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "ReconciliationRejectedByUserId",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "ReconciliationRejectionReason",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "ReconciliationSubmittedAt",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "ReconciliationSubmittedByUserId",
                table: "DispatchOrder");
        }
    }
}
