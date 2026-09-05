using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hma.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppScreen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppScreen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "City",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Company",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bank = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Company", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Department",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Department", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentSequence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastValue = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentSequence", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobTitle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTitle", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Partner",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaxCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperatingFeePercent = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partner", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethod",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethod", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemParameter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemParameter", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tonnage = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employee",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdentityNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VehiclePlate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    JobTitleId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employee_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employee_JobTitle_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitle",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Driver",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdentityNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Driver", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Driver_Partner_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partner",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vehicle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlateNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    VehicleTypeId = table.Column<int>(type: "int", nullable: true),
                    Tonnage = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicle_Partner_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partner",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Vehicle_VehicleType_VehicleTypeId",
                        column: x => x.VehicleTypeId,
                        principalTable: "VehicleType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AppUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    IsManager = table.Column<bool>(type: "bit", nullable: false),
                    IsSpecial = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppUser_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Customer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CityId = table.Column<int>(type: "int", nullable: true),
                    AccountantEmployeeId = table.Column<int>(type: "int", nullable: true),
                    IsWalkIn = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customer_City_CityId",
                        column: x => x.CityId,
                        principalTable: "City",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Customer_Employee_AccountantEmployeeId",
                        column: x => x.AccountantEmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChangeLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangeLog_AppUser_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserPermission",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    AppScreenId = table.Column<int>(type: "int", nullable: false),
                    CanCreate = table.Column<bool>(type: "bit", nullable: false),
                    CanDelete = table.Column<bool>(type: "bit", nullable: false),
                    CanUpdate = table.Column<bool>(type: "bit", nullable: false),
                    CanView = table.Column<bool>(type: "bit", nullable: false),
                    CanPrint = table.Column<bool>(type: "bit", nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPermission_AppScreen_AppScreenId",
                        column: x => x.AppScreenId,
                        principalTable: "AppScreen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPermission_AppUser_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CashPayment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    DriverEmployeeId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    AmountInWords = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayeeName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashPayment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashPayment_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CashPayment_Employee_DriverEmployeeId",
                        column: x => x.DriverEmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashPayment_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DispatchOrder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReconciliationStatus = table.Column<int>(type: "int", nullable: false),
                    ReconciledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReconciledByUserId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    SenderCustomerId = table.Column<int>(type: "int", nullable: true),
                    SenderName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SenderPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SenderAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SenderTaxCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceiverCustomerId = table.Column<int>(type: "int", nullable: true),
                    ReceiverName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceiverPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceiverAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceiverTaxCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PickupAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PickupAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PickupCityId = table.Column<int>(type: "int", nullable: true),
                    DeliveryAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveryCityId = table.Column<int>(type: "int", nullable: true),
                    VehicleId = table.Column<int>(type: "int", nullable: true),
                    DriverId = table.Column<int>(type: "int", nullable: true),
                    VehicleTypeId = table.Column<int>(type: "int", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    PaymentMethodId = table.Column<int>(type: "int", nullable: true),
                    BillingYear = table.Column<int>(type: "int", nullable: false),
                    BillingMonth = table.Column<int>(type: "int", nullable: false),
                    ConfirmedByUserId = table.Column<int>(type: "int", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    Surcharge = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    ExtraCost = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    AmountInWords = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchOrder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchOrder_AppUser_ConfirmedByUserId",
                        column: x => x.ConfirmedByUserId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchOrder_AppUser_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchOrder_AppUser_ReconciledByUserId",
                        column: x => x.ReconciledByUserId,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchOrder_City_DeliveryCityId",
                        column: x => x.DeliveryCityId,
                        principalTable: "City",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchOrder_City_PickupCityId",
                        column: x => x.PickupCityId,
                        principalTable: "City",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchOrder_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchOrder_Customer_ReceiverCustomerId",
                        column: x => x.ReceiverCustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchOrder_Customer_SenderCustomerId",
                        column: x => x.SenderCustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchOrder_Driver_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Driver",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DispatchOrder_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchOrder_PaymentMethod_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "PaymentMethod",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchOrder_VehicleType_VehicleTypeId",
                        column: x => x.VehicleTypeId,
                        principalTable: "VehicleType",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DispatchOrder_Vehicle_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicle",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FreightStatement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TripCount = table.Column<int>(type: "int", nullable: false),
                    FreightTotal = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    SurchargeTotal = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    ExtraCostTotal = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    TotalWithVat = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreightStatement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FreightStatement_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceList",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceList", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceList_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VatInvoice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    IsByCustomer = table.Column<bool>(type: "bit", nullable: false),
                    GoodsName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentMethodText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    AmountInWords = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VatInvoice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VatInvoice_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VatInvoice_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CashReceipt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    DispatchOrderId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    AmountInWords = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashReceipt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashReceipt_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CashReceipt_DispatchOrder_DispatchOrderId",
                        column: x => x.DispatchOrderId,
                        principalTable: "DispatchOrder",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CashReceipt_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DispatchDocument",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DispatchOrderId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoredPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: true),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchDocument", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchDocument_AppUser_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AppUser",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DispatchDocument_DispatchOrder_DispatchOrderId",
                        column: x => x.DispatchOrderId,
                        principalTable: "DispatchOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DispatchOrderLine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DispatchOrderId = table.Column<int>(type: "int", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    GoodsName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PackageCount = table.Column<int>(type: "int", nullable: true),
                    Route = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Kilometers = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchOrderLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchOrderLine_DispatchOrder_DispatchOrderId",
                        column: x => x.DispatchOrderId,
                        principalTable: "DispatchOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FreightStatementLine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FreightStatementId = table.Column<int>(type: "int", nullable: false),
                    DispatchOrderId = table.Column<int>(type: "int", nullable: false),
                    TripDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DispatchCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Route = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlateNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tonnage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DriverName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    Surcharge = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    ExtraCost = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreightStatementLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FreightStatementLine_DispatchOrder_DispatchOrderId",
                        column: x => x.DispatchOrderId,
                        principalTable: "DispatchOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FreightStatementLine_FreightStatement_FreightStatementId",
                        column: x => x.FreightStatementId,
                        principalTable: "FreightStatement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceListRevision",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PriceListId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceListRevision", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceListRevision_Employee_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employee",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PriceListRevision_PriceList_PriceListId",
                        column: x => x.PriceListId,
                        principalTable: "PriceList",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VatInvoiceLine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VatInvoiceId = table.Column<int>(type: "int", nullable: false),
                    DispatchOrderId = table.Column<int>(type: "int", nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VatInvoiceLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VatInvoiceLine_DispatchOrder_DispatchOrderId",
                        column: x => x.DispatchOrderId,
                        principalTable: "DispatchOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VatInvoiceLine_VatInvoice_VatInvoiceId",
                        column: x => x.VatInvoiceId,
                        principalTable: "VatInvoice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceListItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PriceListRevisionId = table.Column<int>(type: "int", nullable: false),
                    PickupCityId = table.Column<int>(type: "int", nullable: true),
                    DeliveryCityId = table.Column<int>(type: "int", nullable: false),
                    VehicleTypeId = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    Surcharge = table.Column<decimal>(type: "decimal(20,2)", precision: 20, scale: 2, nullable: false),
                    LegacyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceListItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceListItem_City_DeliveryCityId",
                        column: x => x.DeliveryCityId,
                        principalTable: "City",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PriceListItem_City_PickupCityId",
                        column: x => x.PickupCityId,
                        principalTable: "City",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PriceListItem_PriceListRevision_PriceListRevisionId",
                        column: x => x.PriceListRevisionId,
                        principalTable: "PriceListRevision",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PriceListItem_VehicleType_VehicleTypeId",
                        column: x => x.VehicleTypeId,
                        principalTable: "VehicleType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUser_EmployeeId",
                table: "AppUser",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CashPayment_CustomerId",
                table: "CashPayment",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CashPayment_DriverEmployeeId",
                table: "CashPayment",
                column: "DriverEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CashPayment_EmployeeId",
                table: "CashPayment",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CashReceipt_CustomerId",
                table: "CashReceipt",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CashReceipt_DispatchOrderId",
                table: "CashReceipt",
                column: "DispatchOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CashReceipt_EmployeeId",
                table: "CashReceipt",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeLog_UserId",
                table: "ChangeLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_AccountantEmployeeId",
                table: "Customer",
                column: "AccountantEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_CityId",
                table: "Customer",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_Code",
                table: "Customer",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchDocument_DispatchOrderId",
                table: "DispatchDocument",
                column: "DispatchOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchDocument_UploadedByUserId",
                table: "DispatchDocument",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_Code",
                table: "DispatchOrder",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_ConfirmedByUserId",
                table: "DispatchOrder",
                column: "ConfirmedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_CreatedByUserId",
                table: "DispatchOrder",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_CustomerId",
                table: "DispatchOrder",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_DeliveryCityId",
                table: "DispatchOrder",
                column: "DeliveryCityId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_DriverId",
                table: "DispatchOrder",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_EmployeeId",
                table: "DispatchOrder",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_PaymentMethodId",
                table: "DispatchOrder",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_PickupCityId",
                table: "DispatchOrder",
                column: "PickupCityId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_ReceiverCustomerId",
                table: "DispatchOrder",
                column: "ReceiverCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_ReconciledByUserId",
                table: "DispatchOrder",
                column: "ReconciledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_SenderCustomerId",
                table: "DispatchOrder",
                column: "SenderCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_VehicleId",
                table: "DispatchOrder",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrder_VehicleTypeId",
                table: "DispatchOrder",
                column: "VehicleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchOrderLine_DispatchOrderId",
                table: "DispatchOrderLine",
                column: "DispatchOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Driver_Code",
                table: "Driver",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Driver_PartnerId",
                table: "Driver",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_DepartmentId",
                table: "Employee",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_JobTitleId",
                table: "Employee",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_FreightStatement_CustomerId_Year_Month",
                table: "FreightStatement",
                columns: new[] { "CustomerId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FreightStatementLine_DispatchOrderId",
                table: "FreightStatementLine",
                column: "DispatchOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FreightStatementLine_FreightStatementId",
                table: "FreightStatementLine",
                column: "FreightStatementId");

            migrationBuilder.CreateIndex(
                name: "IX_Partner_Code",
                table: "Partner",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceList_CustomerId",
                table: "PriceList",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItem_DeliveryCityId",
                table: "PriceListItem",
                column: "DeliveryCityId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItem_PickupCityId",
                table: "PriceListItem",
                column: "PickupCityId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItem_PriceListRevisionId",
                table: "PriceListItem",
                column: "PriceListRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItem_VehicleTypeId",
                table: "PriceListItem",
                column: "VehicleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListRevision_EmployeeId",
                table: "PriceListRevision",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListRevision_PriceListId",
                table: "PriceListRevision",
                column: "PriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermission_AppScreenId",
                table: "UserPermission",
                column: "AppScreenId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermission_AppUserId_AppScreenId",
                table: "UserPermission",
                columns: new[] { "AppUserId", "AppScreenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VatInvoice_CustomerId",
                table: "VatInvoice",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_VatInvoice_EmployeeId",
                table: "VatInvoice",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_VatInvoiceLine_DispatchOrderId",
                table: "VatInvoiceLine",
                column: "DispatchOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VatInvoiceLine_VatInvoiceId",
                table: "VatInvoiceLine",
                column: "VatInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_PartnerId",
                table: "Vehicle",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_PlateNumber",
                table: "Vehicle",
                column: "PlateNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_VehicleTypeId",
                table: "Vehicle",
                column: "VehicleTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashPayment");

            migrationBuilder.DropTable(
                name: "CashReceipt");

            migrationBuilder.DropTable(
                name: "ChangeLog");

            migrationBuilder.DropTable(
                name: "Company");

            migrationBuilder.DropTable(
                name: "DispatchDocument");

            migrationBuilder.DropTable(
                name: "DispatchOrderLine");

            migrationBuilder.DropTable(
                name: "DocumentSequence");

            migrationBuilder.DropTable(
                name: "FreightStatementLine");

            migrationBuilder.DropTable(
                name: "PriceListItem");

            migrationBuilder.DropTable(
                name: "SystemParameter");

            migrationBuilder.DropTable(
                name: "UserPermission");

            migrationBuilder.DropTable(
                name: "VatInvoiceLine");

            migrationBuilder.DropTable(
                name: "FreightStatement");

            migrationBuilder.DropTable(
                name: "PriceListRevision");

            migrationBuilder.DropTable(
                name: "AppScreen");

            migrationBuilder.DropTable(
                name: "DispatchOrder");

            migrationBuilder.DropTable(
                name: "VatInvoice");

            migrationBuilder.DropTable(
                name: "PriceList");

            migrationBuilder.DropTable(
                name: "AppUser");

            migrationBuilder.DropTable(
                name: "Driver");

            migrationBuilder.DropTable(
                name: "PaymentMethod");

            migrationBuilder.DropTable(
                name: "Vehicle");

            migrationBuilder.DropTable(
                name: "Customer");

            migrationBuilder.DropTable(
                name: "Partner");

            migrationBuilder.DropTable(
                name: "VehicleType");

            migrationBuilder.DropTable(
                name: "City");

            migrationBuilder.DropTable(
                name: "Employee");

            migrationBuilder.DropTable(
                name: "Department");

            migrationBuilder.DropTable(
                name: "JobTitle");
        }
    }
}
