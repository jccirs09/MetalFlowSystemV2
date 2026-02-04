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
        public DbSet<Item> Items { get; set; }
        public DbSet<InventoryStock> InventoryStocks { get; set; }
    public DbSet<PackingStation> PackingStations { get; set; }
    public DbSet<UserWorkAssignment> UserWorkAssignments { get; set; }
    public DbSet<AreaShift> AreaShifts { get; set; }
    public DbSet<StationShift> StationShifts { get; set; }
    public DbSet<ShiftAttendance> ShiftAttendances { get; set; }
    public DbSet<PickingList> PickingLists { get; set; }
    public DbSet<PickingListLine> PickingListLines { get; set; }
    public DbSet<PickingListLineReservedMaterial> PickingListLineReservedMaterials { get; set; }
    public DbSet<PackingEvent> PackingEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Item Code Unique
            builder.Entity<Item>()
                .HasIndex(i => i.ItemCode)
                .IsUnique();

            // InventoryStock Indexes (Optimization)
            builder.Entity<InventoryStock>()
                .HasIndex(s => new { s.BranchId, s.IsActive });

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

            // Truck Capacity Precision (already in Attribute but good to enforce here too or rely on attribute)
            // The attribute [Column(TypeName = "decimal(12, 2)")] handles it for SQL Server/SQLite usually,
            // but HasPrecision is cleaner in Fluent API.
            // SQLite treats all numbers as REAL/NUMERIC/INTEGER, but EF Core handles the rounding.
            builder.Entity<Truck>()
                .Property(t => t.CapacityLbs)
                .HasPrecision(12, 2);

            // Phase 3: Picking Lists
            builder.Entity<PickingList>()
                .HasIndex(pl => new { pl.BranchId, pl.PickingListNumber })
                .IsUnique();

            builder.Entity<PickingListLine>()
                .HasIndex(l => new { l.PickingListId, l.LineNumber })
                .IsUnique();

            builder.Entity<PickingListLineReservedMaterial>()
                .HasIndex(r => new { r.PickingListLineId, r.TagNumber })
                .IsUnique();
        }
    }
}
