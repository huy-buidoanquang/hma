using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hma.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationAndCustomerAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerAlias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Alias = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAlias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerAlias_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocationAlias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Alias = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationAlias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationAlias_City_CityId",
                        column: x => x.CityId,
                        principalTable: "City",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAlias_Alias",
                table: "CustomerAlias",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAlias_CustomerId",
                table: "CustomerAlias",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationAlias_Alias",
                table: "LocationAlias",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationAlias_CityId",
                table: "LocationAlias",
                column: "CityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerAlias");

            migrationBuilder.DropTable(
                name: "LocationAlias");
        }
    }
}
