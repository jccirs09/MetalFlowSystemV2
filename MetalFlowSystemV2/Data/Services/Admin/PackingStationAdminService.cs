using Microsoft.EntityFrameworkCore;
using MetalFlowSystemV2.Data.Entities;

namespace MetalFlowSystemV2.Data.Services.Admin
{
    public class PackingStationAdminService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public PackingStationAdminService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<PackingStation>> GetByBranchIdAsync(int branchId)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.PackingStations
                .Where(p => p.BranchId == branchId)
                .OrderBy(p => p.Code)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task CreateAsync(PackingStation station)
        {
            using var context = _contextFactory.CreateDbContext();
            station.Code = station.Code.ToUpperInvariant();

            if (await context.PackingStations.AnyAsync(p => p.BranchId == station.BranchId && p.Code == station.Code))
            {
                throw new InvalidOperationException($"Packing Station code '{station.Code}' already exists in this branch.");
            }

            context.PackingStations.Add(station);
            await context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PackingStation station)
        {
            using var context = _contextFactory.CreateDbContext();
            station.Code = station.Code.ToUpperInvariant();

            var existing = await context.PackingStations.FindAsync(station.Id);
            if (existing == null) throw new KeyNotFoundException("Packing Station not found");

            if (existing.Code != station.Code && await context.PackingStations.AnyAsync(p => p.BranchId == station.BranchId && p.Code == station.Code))
            {
                throw new InvalidOperationException($"Packing Station code '{station.Code}' already exists in this branch.");
            }

            existing.Code = station.Code;
            existing.Name = station.Name;
            existing.IsActive = station.IsActive;
            // BranchId cannot change

            await context.SaveChangesAsync();
        }
    }
}
