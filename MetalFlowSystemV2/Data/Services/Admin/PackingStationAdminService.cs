using Microsoft.EntityFrameworkCore;
using MetalFlowSystemV2.Data.Entities;

namespace MetalFlowSystemV2.Data.Services.Admin
{
    public class PackingStationAdminService
    {
        private readonly ApplicationDbContext _context;

        public PackingStationAdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PackingStation>> GetByBranchIdAsync(int branchId)
        {
            return await _context.PackingStations
                .Where(p => p.BranchId == branchId)
                .OrderBy(p => p.Code)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task CreateAsync(PackingStation station)
        {
            station.Code = station.Code.ToUpperInvariant();

            if (await _context.PackingStations.AnyAsync(p => p.BranchId == station.BranchId && p.Code == station.Code))
            {
                throw new InvalidOperationException($"Packing Station code '{station.Code}' already exists in this branch.");
            }

            _context.PackingStations.Add(station);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PackingStation station)
        {
            station.Code = station.Code.ToUpperInvariant();

            var existing = await _context.PackingStations.FindAsync(station.Id);
            if (existing == null) throw new KeyNotFoundException("Packing Station not found");

            if (existing.Code != station.Code && await _context.PackingStations.AnyAsync(p => p.BranchId == station.BranchId && p.Code == station.Code))
            {
                throw new InvalidOperationException($"Packing Station code '{station.Code}' already exists in this branch.");
            }

            existing.Code = station.Code;
            existing.Name = station.Name;
            existing.IsActive = station.IsActive;
            // BranchId cannot change

            await _context.SaveChangesAsync();
        }
    }
}
