namespace NumuneStok.Models
{
    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ContactName { get; set; }
        public string? WalletAddress { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class Carrier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ContactName { get; set; }
        public string? WalletAddress { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class WarehouseLocation
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? WalletAddress { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class LaboratoryLocation
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string? WalletAddress { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
