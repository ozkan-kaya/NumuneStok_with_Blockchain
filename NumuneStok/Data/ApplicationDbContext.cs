using System.Data;
using Microsoft.EntityFrameworkCore;
using NumuneStok.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ChildProduct> ChildProducts { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Carrier> Carriers { get; set; }
    public DbSet<WarehouseLocation> WarehouseLocations { get; set; }
    public DbSet<LaboratoryLocation> LaboratoryLocations { get; set; }
    public DbSet<SupplyChainShipment> SupplyChainShipments { get; set; }
    public DbSet<SupplyChainTransfer> SupplyChainTransfers { get; set; }
    public DbSet<SupplyChainReceipt> SupplyChainReceipts { get; set; }
}
