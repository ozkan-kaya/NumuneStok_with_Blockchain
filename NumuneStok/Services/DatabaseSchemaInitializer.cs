using System.Data;
using Microsoft.EntityFrameworkCore;

namespace NumuneStok.Services
{
    public static class DatabaseSchemaInitializer
    {
        public static async Task EnsureSupplyChainSchemaAsync(ApplicationDbContext context)
        {
            await context.Database.OpenConnectionAsync();

            try
            {
                await EnsureColumnAsync(context, "Users", "BlockchainRole", "ALTER TABLE `Users` ADD COLUMN `BlockchainRole` longtext NULL;");
                await EnsureColumnAsync(context, "Users", "WalletAddress", "ALTER TABLE `Users` ADD COLUMN `WalletAddress` longtext NULL;");

                await ExecuteNonQueryAsync(context, @"
CREATE TABLE IF NOT EXISTS `Suppliers` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` longtext NOT NULL,
  `ContactName` longtext NULL,
  `WalletAddress` longtext NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
);");

                await ExecuteNonQueryAsync(context, @"
CREATE TABLE IF NOT EXISTS `Carriers` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` longtext NOT NULL,
  `ContactName` longtext NULL,
  `WalletAddress` longtext NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
);");

                await ExecuteNonQueryAsync(context, @"
CREATE TABLE IF NOT EXISTS `WarehouseLocations` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` longtext NOT NULL,
  `Address` longtext NULL,
  `WalletAddress` longtext NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
);");

                await ExecuteNonQueryAsync(context, @"
CREATE TABLE IF NOT EXISTS `LaboratoryLocations` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` longtext NOT NULL,
  `Department` longtext NULL,
  `WalletAddress` longtext NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
);");

                await ExecuteNonQueryAsync(context, @"
CREATE TABLE IF NOT EXISTS `SupplyChainReceipts` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ProductId` int NOT NULL,
  `ChildProductId` int NULL,
  `LotNumber` longtext NOT NULL,
  `Quantity` int NOT NULL,
  `FromLocation` longtext NOT NULL,
  `ToLocation` longtext NOT NULL,
  `Status` int NOT NULL,
  `BlockchainTransactionHash` longtext NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `CompletedAt` datetime(6) NULL,
  PRIMARY KEY (`Id`),
  INDEX `IX_SupplyChainReceipts_ProductId` (`ProductId`),
  INDEX `IX_SupplyChainReceipts_ChildProductId` (`ChildProductId`)
);");

                await ExecuteNonQueryAsync(context, @"
CREATE TABLE IF NOT EXISTS `SupplyChainShipments` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ProductId` int NOT NULL,
  `ChildProductId` int NULL,
  `SupplierId` int NULL,
  `CarrierId` int NULL,
  `WarehouseLocationId` int NULL,
  `LotNumber` longtext NOT NULL,
  `Quantity` int NOT NULL,
  `Status` int NOT NULL,
  `BlockchainTransactionHash` longtext NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `ShippedAt` datetime(6) NULL,
  `ReceivedAt` datetime(6) NULL,
  PRIMARY KEY (`Id`),
  INDEX `IX_SupplyChainShipments_ProductId` (`ProductId`),
  INDEX `IX_SupplyChainShipments_ChildProductId` (`ChildProductId`),
  INDEX `IX_SupplyChainShipments_SupplierId` (`SupplierId`),
  INDEX `IX_SupplyChainShipments_CarrierId` (`CarrierId`),
  INDEX `IX_SupplyChainShipments_WarehouseLocationId` (`WarehouseLocationId`)
);");

                await ExecuteNonQueryAsync(context, @"
CREATE TABLE IF NOT EXISTS `SupplyChainTransfers` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ProductId` int NOT NULL,
  `ChildProductId` int NULL,
  `FromWarehouseLocationId` int NULL,
  `ToLaboratoryLocationId` int NULL,
  `LotNumber` longtext NOT NULL,
  `Quantity` int NOT NULL,
  `Status` int NOT NULL,
  `BlockchainTransactionHash` longtext NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `TransferredAt` datetime(6) NULL,
  `ConsumedAt` datetime(6) NULL,
  PRIMARY KEY (`Id`),
  INDEX `IX_SupplyChainTransfers_ProductId` (`ProductId`),
  INDEX `IX_SupplyChainTransfers_ChildProductId` (`ChildProductId`),
  INDEX `IX_SupplyChainTransfers_FromWarehouseLocationId` (`FromWarehouseLocationId`),
  INDEX `IX_SupplyChainTransfers_ToLaboratoryLocationId` (`ToLaboratoryLocationId`)
);");
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
        }

        private static async Task EnsureColumnAsync(ApplicationDbContext context, string tableName, string columnName, string alterSql)
        {
            if (!await ColumnExistsAsync(context, tableName, columnName))
            {
                await ExecuteNonQueryAsync(context, alterSql);
            }
        }

        private static async Task<bool> ColumnExistsAsync(ApplicationDbContext context, string tableName, string columnName)
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = @"
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND LOWER(TABLE_NAME) = LOWER(@tableName)
  AND LOWER(COLUMN_NAME) = LOWER(@columnName);";

            var tableParameter = command.CreateParameter();
            tableParameter.ParameterName = "@tableName";
            tableParameter.Value = tableName;
            command.Parameters.Add(tableParameter);

            var columnParameter = command.CreateParameter();
            columnParameter.ParameterName = "@columnName";
            columnParameter.Value = columnName;
            command.Parameters.Add(columnParameter);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        private static async Task ExecuteNonQueryAsync(ApplicationDbContext context, string sql)
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            await command.ExecuteNonQueryAsync();
        }
    }
}
