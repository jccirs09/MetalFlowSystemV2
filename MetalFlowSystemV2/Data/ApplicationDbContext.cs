using MetalFlowSystemV2.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MetalFlowSystemV2.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Branch> Branches { get; set; }
        public DbSet<ProductionArea> ProductionAreas { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<Truck> Trucks { get; set; }
        public DbSet<UserBranch> UserBranches { get; set; }

        // Phase 1 Entities
        public DbSet<Item> Items { get; set; }
        public DbSet<InventoryStock> InventoryStocks { get; set; }
        public DbSet<PackingStation> PackingStations { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // UserBranch Unique Constraint (One Role per Branch per User)
            builder.Entity<UserBranch>()
                .HasIndex(ub => new { ub.UserId, ub.BranchId })
                .IsUnique();

            // Branch Code Unique
            builder.Entity<Branch>()
                .HasIndex(b => b.Code)
                .IsUnique();

            // ProductionArea (BranchId + Code) Unique
            builder.Entity<ProductionArea>()
                .HasIndex(p => new { p.BranchId, p.Code })
                .IsUnique();

            // Shift (BranchId + Code) Unique
            builder.Entity<Shift>()
                .HasIndex(s => new { s.BranchId, s.Code })
                .IsUnique();

            // Truck (BranchId + TruckCode) Unique
            builder.Entity<Truck>()
                .HasIndex(t => new { t.BranchId, t.TruckCode })
                .IsUnique();

            // Truck Capacity Precision
            builder.Entity<Truck>()
                .Property(t => t.CapacityLbs)
                .HasPrecision(12, 2);

            // --- Phase 1: Item Master ---
            builder.Entity<Item>()
                .HasIndex(i => i.ItemCode)
                .IsUnique();

            builder.Entity<Item>()
                .HasOne(i => i.ParentItem)
                .WithMany(i => i.ChildItems)
                .HasForeignKey(i => i.ParentItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Item>()
                .Property(i => i.WeightPerUnit)
                .HasPrecision(12, 3);

            // --- Phase 1: Inventory Stock ---
            // Unique Index on (BranchId, ItemId, LocationCode) - Filtered for Active
            builder.Entity<InventoryStock>()
                .HasIndex(s => new { s.BranchId, s.ItemId, s.LocationCode })
                .IsUnique()
                .HasFilter("[IsActive] = 1");

            builder.Entity<InventoryStock>()
                .Property(s => s.QuantityOnHand)
                .HasPrecision(18, 3);

            builder.Entity<InventoryStock>()
                .Property(s => s.WeightOnHand)
                .HasPrecision(18, 3);

            // --- Phase 1: Packing Stations ---
            builder.Entity<PackingStation>()
                .HasIndex(ps => new { ps.BranchId, ps.StationName })
                .IsUnique();

            // Unique StationCode per Branch if provided
            builder.Entity<PackingStation>()
                .HasIndex(ps => new { ps.BranchId, ps.StationCode })
                .IsUnique()
                .HasFilter("[StationCode] IS NOT NULL");
        }
    }
}
