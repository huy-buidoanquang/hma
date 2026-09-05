using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hma.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationsAndRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DispatchOrder_City_DeliveryCityId",
                table: "DispatchOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchOrder_City_PickupCityId",
                table: "DispatchOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_LocationAlias_City_CityId",
                table: "LocationAlias");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceListItem_City_DeliveryCityId",
                table: "PriceListItem");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceListItem_City_PickupCityId",
                table: "PriceListItem");

            migrationBuilder.AddColumn<bool>(
                name: "HasPriceFluctuation",
                table: "PriceList",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RouteId",
                table: "DispatchOrder",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RouteId",
                table: "PriceListItem",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "LocationAlias",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Location",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CityId = table.Column<int>(type: "int", nullable: true),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Location", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Location_City_CityId",
                        column: x => x.CityId,
                        principalTable: "City",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Route",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Fingerprint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Route", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DispatchOrderStop",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DispatchOrderId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    NameSnapshot = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchOrderStop", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchOrderStop_DispatchOrder_DispatchOrderId",
                        column: x => x.DispatchOrderId,
                        principalTable: "DispatchOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DispatchOrderStop_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RouteAlias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Alias = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RouteId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteAlias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteAlias_Route_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Route",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RouteStop",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RouteId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteStop", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteStop_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RouteStop_Route_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Route",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO Location (Code, Name, Description, CityId, LegacyId)
                SELECT c.Code, c.Name, c.Description, c.Id, c.LegacyId
                FROM City c
                WHERE EXISTS (SELECT 1 FROM DispatchOrder o WHERE o.PickupCityId = c.Id OR o.DeliveryCityId = c.Id)
                   OR EXISTS (SELECT 1 FROM PriceListItem i WHERE i.PickupCityId = c.Id OR i.DeliveryCityId = c.Id)
                   OR EXISTS (SELECT 1 FROM LocationAlias a WHERE a.CityId = c.Id);

                UPDATE a SET LocationId = l.Id
                FROM LocationAlias a
                INNER JOIN Location l ON l.CityId = a.CityId;

                INSERT INTO Route (Code, Name, Fingerprint)
                SELECT DISTINCT
                    LEFT(CONCAT(pl.Code, N'-', dl.Code, N'-', pl.Id, N'-', dl.Id), 50),
                    CONCAT(pl.Name, N' → ', dl.Name),
                    CONCAT(pl.Id, N'-', dl.Id)
                FROM (
                    SELECT PickupCityId AS Pickup, DeliveryCityId AS Delivery
                    FROM DispatchOrder
                    WHERE PickupCityId IS NOT NULL AND DeliveryCityId IS NOT NULL
                    UNION
                    SELECT PickupCityId, DeliveryCityId
                    FROM PriceListItem
                    WHERE PickupCityId IS NOT NULL
                ) p
                INNER JOIN Location pl ON pl.CityId = p.Pickup
                INNER JOIN Location dl ON dl.CityId = p.Delivery
                WHERE NOT EXISTS (
                    SELECT 1 FROM Route r WHERE r.Fingerprint = CONCAT(pl.Id, N'-', dl.Id));

                INSERT INTO RouteStop (RouteId, Sequence, LocationId)
                SELECT r.Id, 0, pl.Id
                FROM (
                    SELECT PickupCityId AS Pickup, DeliveryCityId AS Delivery
                    FROM DispatchOrder
                    WHERE PickupCityId IS NOT NULL AND DeliveryCityId IS NOT NULL
                    UNION
                    SELECT PickupCityId, DeliveryCityId
                    FROM PriceListItem
                    WHERE PickupCityId IS NOT NULL
                ) p
                INNER JOIN Location pl ON pl.CityId = p.Pickup
                INNER JOIN Location dl ON dl.CityId = p.Delivery
                INNER JOIN Route r ON r.Fingerprint = CONCAT(pl.Id, N'-', dl.Id)
                WHERE NOT EXISTS (SELECT 1 FROM RouteStop s WHERE s.RouteId = r.Id AND s.Sequence = 0);

                INSERT INTO RouteStop (RouteId, Sequence, LocationId)
                SELECT r.Id, 1, dl.Id
                FROM (
                    SELECT PickupCityId AS Pickup, DeliveryCityId AS Delivery
                    FROM DispatchOrder
                    WHERE PickupCityId IS NOT NULL AND DeliveryCityId IS NOT NULL
                    UNION
                    SELECT PickupCityId, DeliveryCityId
                    FROM PriceListItem
                    WHERE PickupCityId IS NOT NULL
                ) p
                INNER JOIN Location pl ON pl.CityId = p.Pickup
                INNER JOIN Location dl ON dl.CityId = p.Delivery
                INNER JOIN Route r ON r.Fingerprint = CONCAT(pl.Id, N'-', dl.Id)
                WHERE NOT EXISTS (SELECT 1 FROM RouteStop s WHERE s.RouteId = r.Id AND s.Sequence = 1);

                UPDATE o SET RouteId = r.Id
                FROM DispatchOrder o
                INNER JOIN Location pl ON pl.CityId = o.PickupCityId
                INNER JOIN Location dl ON dl.CityId = o.DeliveryCityId
                INNER JOIN Route r ON r.Fingerprint = CONCAT(pl.Id, N'-', dl.Id);

                INSERT INTO DispatchOrderStop (DispatchOrderId, Sequence, LocationId, NameSnapshot)
                SELECT o.Id, 0, pl.Id, pl.Name
                FROM DispatchOrder o
                INNER JOIN Location pl ON pl.CityId = o.PickupCityId
                WHERE o.RouteId IS NOT NULL
                UNION ALL
                SELECT o.Id, 1, dl.Id, dl.Name
                FROM DispatchOrder o
                INNER JOIN Location dl ON dl.CityId = o.DeliveryCityId
                WHERE o.RouteId IS NOT NULL;

                UPDATE i SET RouteId = r.Id
                FROM PriceListItem i
                INNER JOIN Location pl ON pl.CityId = i.PickupCityId
                INNER JOIN Location dl ON dl.CityId = i.DeliveryCityId
                INNER JOIN Route r ON r.Fingerprint = CONCAT(pl.Id, N'-', dl.Id);

                DELETE FROM PriceListItem WHERE RouteId IS NULL;
                DELETE FROM LocationAlias WHERE LocationId IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "RouteId",
                table: "PriceListItem",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LocationId",
                table: "LocationAlias",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_PriceListItem_PickupCityId",
                table: "PriceListItem");

            migrationBuilder.DropIndex(
                name: "IX_PriceListItem_DeliveryCityId",
                table: "PriceListItem");

            migrationBuilder.DropIndex(
                name: "IX_DispatchOrder_PickupCityId",
                table: "DispatchOrder");

            migrationBuilder.DropIndex(
                name: "IX_DispatchOrder_DeliveryCityId",
                table: "DispatchOrder");

            migrationBuilder.DropIndex(
                name: "IX_LocationAlias_CityId",
                table: "LocationAlias");

            migrationBuilder.DropColumn(
                name: "PickupCityId",
                table: "PriceListItem");

            migrationBuilder.DropColumn(
                name: "DeliveryCityId",
                table: "PriceListItem");

            migrationBuilder.DropColumn(
                name: "PickupCityId",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "DeliveryCityId",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "LocationAlias");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_RouteId",
                table: "DispatchOrder",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrderStop_DispatchOrderId_Sequence",
                table: "DispatchOrderStop",
                columns: new[] { "DispatchOrderId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrderStop_LocationId",
                table: "DispatchOrderStop",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Location_CityId",
                table: "Location",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Location_Code",
                table: "Location",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationAlias_LocationId",
                table: "LocationAlias",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItem_RouteId",
                table: "PriceListItem",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Route_Code",
                table: "Route",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Route_Fingerprint",
                table: "Route",
                column: "Fingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteAlias_Alias",
                table: "RouteAlias",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteAlias_RouteId",
                table: "RouteAlias",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteStop_LocationId",
                table: "RouteStop",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteStop_RouteId_Sequence",
                table: "RouteStop",
                columns: new[] { "RouteId", "Sequence" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchOrder_Route_RouteId",
                table: "DispatchOrder",
                column: "RouteId",
                principalTable: "Route",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LocationAlias_Location_LocationId",
                table: "LocationAlias",
                column: "LocationId",
                principalTable: "Location",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceListItem_Route_RouteId",
                table: "PriceListItem",
                column: "RouteId",
                principalTable: "Route",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DispatchOrder_Route_RouteId",
                table: "DispatchOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_LocationAlias_Location_LocationId",
                table: "LocationAlias");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceListItem_Route_RouteId",
                table: "PriceListItem");

            migrationBuilder.DropTable(
                name: "DispatchOrderStop");

            migrationBuilder.DropTable(
                name: "RouteAlias");

            migrationBuilder.DropTable(
                name: "RouteStop");

            migrationBuilder.DropIndex(
                name: "IX_DispatchOrder_RouteId",
                table: "DispatchOrder");

            migrationBuilder.DropIndex(
                name: "IX_LocationAlias_LocationId",
                table: "LocationAlias");

            migrationBuilder.DropIndex(
                name: "IX_PriceListItem_RouteId",
                table: "PriceListItem");

            migrationBuilder.DropColumn(
                name: "HasPriceFluctuation",
                table: "PriceList");

            migrationBuilder.DropColumn(
                name: "RouteId",
                table: "DispatchOrder");

            migrationBuilder.DropColumn(
                name: "RouteId",
                table: "PriceListItem");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "LocationAlias");

            migrationBuilder.DropTable(
                name: "Location");

            migrationBuilder.DropTable(
                name: "Route");

            migrationBuilder.AddColumn<int>(
                name: "PickupCityId",
                table: "PriceListItem",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryCityId",
                table: "PriceListItem",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PickupCityId",
                table: "DispatchOrder",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryCityId",
                table: "DispatchOrder",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "LocationAlias",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItem_PickupCityId",
                table: "PriceListItem",
                column: "PickupCityId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItem_DeliveryCityId",
                table: "PriceListItem",
                column: "DeliveryCityId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_PickupCityId",
                table: "DispatchOrder",
                column: "PickupCityId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_DeliveryCityId",
                table: "DispatchOrder",
                column: "DeliveryCityId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationAlias_CityId",
                table: "LocationAlias",
                column: "CityId");

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchOrder_City_DeliveryCityId",
                table: "DispatchOrder",
                column: "DeliveryCityId",
                principalTable: "City",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchOrder_City_PickupCityId",
                table: "DispatchOrder",
                column: "PickupCityId",
                principalTable: "City",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LocationAlias_City_CityId",
                table: "LocationAlias",
                column: "CityId",
                principalTable: "City",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceListItem_City_DeliveryCityId",
                table: "PriceListItem",
                column: "DeliveryCityId",
                principalTable: "City",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceListItem_City_PickupCityId",
                table: "PriceListItem",
                column: "PickupCityId",
                principalTable: "City",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
