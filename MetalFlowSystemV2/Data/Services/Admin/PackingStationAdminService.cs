using MetalFlowSystemV2.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalFlowSystemV2.Data.Services.Admin
{
    public class PackingStationAdminService
    {
        private readonly ApplicationDbContext _context;

        public PackingStationAdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PackingStation>> GetByBranchAsync(int branchId)
        {
            return await _context.PackingStations
                .Where(ps => ps.BranchId == branchId)
                .OrderBy(ps => ps.SortOrder)
                .ThenBy(ps => ps.StationName)
                .ToListAsync();
        }

        public async Task<PackingStation?> GetByIdAsync(int id)
        {
            return await _context.PackingStations.FindAsync(id);
        }

        public async Task<PackingStation> CreateAsync(PackingStation station)
        {
            await ValidateStationAsync(station);
            _context.PackingStations.Add(station);
            await _context.SaveChangesAsync();
            return station;
        }

        public async Task<PackingStation> UpdateAsync(PackingStation station)
        {
            await ValidateStationAsync(station);
            _context.PackingStations.Update(station);
            await _context.SaveChangesAsync();
            return station;
        }

        public async Task DeactivateAsync(int id)
        {
            var station = await _context.PackingStations.FindAsync(id);
            if (station != null)
            {
                station.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        private async Task ValidateStationAsync(PackingStation station)
        {
            // Unique Name per Branch
            var existingName = await _context.PackingStations
                .Where(ps => ps.BranchId == station.BranchId && ps.StationName == station.StationName && ps.Id != station.Id)
                .FirstOrDefaultAsync();
            if (existingName != null)
            {
                throw new Exception($"Packing Station '{station.StationName}' already exists in this branch.");
            }

            // Unique Code per Branch (if provided)
            if (!string.IsNullOrEmpty(station.StationCode))
            {
                var existingCode = await _context.PackingStations
                    .Where(ps => ps.BranchId == station.BranchId && ps.StationCode == station.StationCode && ps.Id != station.Id)
                    .FirstOrDefaultAsync();
                if (existingCode != null)
                {
                    throw new Exception($"Station Code '{station.StationCode}' already exists in this branch.");
                }
            }
        }
    }
}
