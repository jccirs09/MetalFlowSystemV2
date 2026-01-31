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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

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
        }
    }
}
