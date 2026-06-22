using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NumuneStok.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplyChainManagementModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlockchainRole",
                table: "Users",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WalletAddress",
                table: "Users",
                type: "longtext",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Carriers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false),
                    ContactName = table.Column<string>(type: "longtext", nullable: true),
                    WalletAddress = table.Column<string>(type: "longtext", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carriers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LaboratoryLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false),
                    Department = table.Column<string>(type: "longtext", nullable: true),
                    WalletAddress = table.Column<string>(type: "longtext", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaboratoryLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false),
                    ContactName = table.Column<string>(type: "longtext", nullable: true),
                    WalletAddress = table.Column<string>(type: "longtext", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false),
                    Address = table.Column<string>(type: "longtext", nullable: true),
                    WalletAddress = table.Column<string>(type: "longtext", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplyChainReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ChildProductId = table.Column<int>(type: "int", nullable: true),
                    LotNumber = table.Column<string>(type: "longtext", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    FromLocation = table.Column<string>(type: "longtext", nullable: false),
                    ToLocation = table.Column<string>(type: "longtext", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BlockchainTransactionHash = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplyChainReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplyChainReceipts_ChildProducts_ChildProductId",
                        column: x => x.ChildProductId,
                        principalTable: "ChildProducts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplyChainReceipts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplyChainShipments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ChildProductId = table.Column<int>(type: "int", nullable: true),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    CarrierId = table.Column<int>(type: "int", nullable: true),
                    WarehouseLocationId = table.Column<int>(type: "int", nullable: true),
                    LotNumber = table.Column<string>(type: "longtext", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BlockchainTransactionHash = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ShippedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplyChainShipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplyChainShipments_Carriers_CarrierId",
                        column: x => x.CarrierId,
                        principalTable: "Carriers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplyChainShipments_ChildProducts_ChildProductId",
                        column: x => x.ChildProductId,
                        principalTable: "ChildProducts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplyChainShipments_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplyChainShipments_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplyChainShipments_WarehouseLocations_WarehouseLocationId",
                        column: x => x.WarehouseLocationId,
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupplyChainTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ChildProductId = table.Column<int>(type: "int", nullable: true),
                    FromWarehouseLocationId = table.Column<int>(type: "int", nullable: true),
                    ToLaboratoryLocationId = table.Column<int>(type: "int", nullable: true),
                    LotNumber = table.Column<string>(type: "longtext", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BlockchainTransactionHash = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TransferredAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ConsumedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplyChainTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplyChainTransfers_ChildProducts_ChildProductId",
                        column: x => x.ChildProductId,
                        principalTable: "ChildProducts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplyChainTransfers_LaboratoryLocations_ToLaboratoryLocati~",
                        column: x => x.ToLaboratoryLocationId,
                        principalTable: "LaboratoryLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupplyChainTransfers_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplyChainTransfers_WarehouseLocations_FromWarehouseLocati~",
                        column: x => x.FromWarehouseLocationId,
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplyChainReceipts_ChildProductId",
                table: "SupplyChainReceipts",
                column: "ChildProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyChainReceipts_ProductId",
                table: "SupplyChainReceipts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyChainShipments_CarrierId",
                table: "SupplyChainShipments",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyChainShipments_ChildProductId",
                table: "SupplyChainShipments",
                column: "ChildProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyChainShipments_ProductId",
                table: "SupplyChainShipments",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyChainShipments_SupplierId",
                table: "SupplyChainShipments",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyChainShipments_WarehouseLocationId",
                table: "SupplyChainShipments",
                column: "WarehouseLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyChainTransfers_ChildProductId",
                table: "SupplyChainTransfers",
                column: "ChildProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyChainTransfers_FromWarehouseLocationId",
                table: "SupplyChainTransfers",
                column: "FromWarehouseLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyChainTransfers_ProductId",
                table: "SupplyChainTransfers",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyChainTransfers_ToLaboratoryLocationId",
                table: "SupplyChainTransfers",
                column: "ToLaboratoryLocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SupplyChainReceipts");
            migrationBuilder.DropTable(name: "SupplyChainShipments");
            migrationBuilder.DropTable(name: "SupplyChainTransfers");
            migrationBuilder.DropTable(name: "Carriers");
            migrationBuilder.DropTable(name: "Suppliers");
            migrationBuilder.DropTable(name: "LaboratoryLocations");
            migrationBuilder.DropTable(name: "WarehouseLocations");

            migrationBuilder.DropColumn(name: "BlockchainRole", table: "Users");
            migrationBuilder.DropColumn(name: "WalletAddress", table: "Users");
        }
    }
}
