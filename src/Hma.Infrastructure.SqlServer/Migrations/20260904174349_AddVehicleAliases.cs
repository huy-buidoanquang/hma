using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hma.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleAlias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Alias = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleAlias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleAlias_Vehicle_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAlias_Alias",
                table: "VehicleAlias",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAlias_VehicleId",
                table: "VehicleAlias",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleAlias");
        }
    }
}
