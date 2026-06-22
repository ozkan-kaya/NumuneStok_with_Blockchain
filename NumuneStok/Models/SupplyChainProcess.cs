namespace NumuneStok.Models
{
    public enum SupplyChainProcessStatus
    {
        Created = 0,
        Produced = 1,
        Shipped = 2,
        Received = 3,
        Transferred = 4,
        Consumed = 5,
        HeldInStock = 6,
        Failed = 7
    }

    public class SupplyChainShipment
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int? ChildProductId { get; set; }
        public ChildProduct? ChildProduct { get; set; }
        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }
        public int? CarrierId { get; set; }
        public Carrier? Carrier { get; set; }
        public int? WarehouseLocationId { get; set; }
        public WarehouseLocation? WarehouseLocation { get; set; }
        public string LotNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public SupplyChainProcessStatus Status { get; set; } = SupplyChainProcessStatus.Created;
        public string? BlockchainTransactionHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ShippedAt { get; set; }
        public DateTime? ReceivedAt { get; set; }
    }

    public class SupplyChainTransfer
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int? ChildProductId { get; set; }
        public ChildProduct? ChildProduct { get; set; }
        public int? FromWarehouseLocationId { get; set; }
        public WarehouseLocation? FromWarehouseLocation { get; set; }
        public int? ToLaboratoryLocationId { get; set; }
        public LaboratoryLocation? ToLaboratoryLocation { get; set; }
        public string LotNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public SupplyChainProcessStatus Status { get; set; } = SupplyChainProcessStatus.Created;
        public string? BlockchainTransactionHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? TransferredAt { get; set; }
        public DateTime? ConsumedAt { get; set; }
    }

    public class SupplyChainReceipt
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int? ChildProductId { get; set; }
        public ChildProduct? ChildProduct { get; set; }
        public string LotNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
        public SupplyChainProcessStatus Status { get; set; } = SupplyChainProcessStatus.Created;
        public string? BlockchainTransactionHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
