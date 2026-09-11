using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hma.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddStatementWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT DispatchOrderId
                    FROM dbo.FreightStatementLine
                    GROUP BY DispatchOrderId
                    HAVING COUNT(*) > 1)
                    THROW 51001, N'Không thể áp dụng migration: một lệnh đang nằm trong nhiều dòng bảng kê. Chạy báo cáo tiền kiểm và xử lý dữ liệu trùng trước.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_FreightStatementLine_DispatchOrderId",
                table: "FreightStatementLine");

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "FreightStatement",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinalizedAt",
                table: "FreightStatement",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinalizedByUserId",
                table: "FreightStatement",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "FreightStatement",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "FreightStatement",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmittedByUserId",
                table: "FreightStatement",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "FreightStatement",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "FreightStatement",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VoidedByUserId",
                table: "FreightStatement",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FreightStatementLine_DispatchOrderId",
                table: "FreightStatementLine",
                column: "DispatchOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FreightStatement_CreatedByUserId",
                table: "FreightStatement",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FreightStatement_FinalizedByUserId",
                table: "FreightStatement",
                column: "FinalizedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FreightStatement_SubmittedByUserId",
                table: "FreightStatement",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FreightStatement_VoidedByUserId",
                table: "FreightStatement",
                column: "VoidedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FreightStatement_AppUser_CreatedByUserId",
                table: "FreightStatement",
                column: "CreatedByUserId",
                principalTable: "AppUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FreightStatement_AppUser_FinalizedByUserId",
                table: "FreightStatement",
                column: "FinalizedByUserId",
                principalTable: "AppUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FreightStatement_AppUser_SubmittedByUserId",
                table: "FreightStatement",
                column: "SubmittedByUserId",
                principalTable: "AppUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FreightStatement_AppUser_VoidedByUserId",
                table: "FreightStatement",
                column: "VoidedByUserId",
                principalTable: "AppUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FreightStatement_AppUser_CreatedByUserId",
                table: "FreightStatement");

            migrationBuilder.DropForeignKey(
                name: "FK_FreightStatement_AppUser_FinalizedByUserId",
                table: "FreightStatement");

            migrationBuilder.DropForeignKey(
                name: "FK_FreightStatement_AppUser_SubmittedByUserId",
                table: "FreightStatement");

            migrationBuilder.DropForeignKey(
                name: "FK_FreightStatement_AppUser_VoidedByUserId",
                table: "FreightStatement");

            migrationBuilder.DropIndex(
                name: "IX_FreightStatementLine_DispatchOrderId",
                table: "FreightStatementLine");

            migrationBuilder.DropIndex(
                name: "IX_FreightStatement_CreatedByUserId",
                table: "FreightStatement");

            migrationBuilder.DropIndex(
                name: "IX_FreightStatement_FinalizedByUserId",
                table: "FreightStatement");

            migrationBuilder.DropIndex(
                name: "IX_FreightStatement_SubmittedByUserId",
                table: "FreightStatement");

            migrationBuilder.DropIndex(
                name: "IX_FreightStatement_VoidedByUserId",
                table: "FreightStatement");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "FreightStatement");

            migrationBuilder.DropColumn(
                name: "FinalizedAt",
                table: "FreightStatement");

            migrationBuilder.DropColumn(
                name: "FinalizedByUserId",
                table: "FreightStatement");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "FreightStatement");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "FreightStatement");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "FreightStatement");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "FreightStatement");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "FreightStatement");

            migrationBuilder.DropColumn(
                name: "VoidedByUserId",
                table: "FreightStatement");

            migrationBuilder.CreateIndex(
                name: "IX_FreightStatementLine_DispatchOrderId",
                table: "FreightStatementLine",
                column: "DispatchOrderId");
        }
    }
}
