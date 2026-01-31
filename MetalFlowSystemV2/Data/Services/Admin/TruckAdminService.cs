using MetalFlowSystemV2.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalFlowSystemV2.Data.Services.Admin;

public class TruckAdminService
{
    private readonly ApplicationDbContext _context;

    public TruckAdminService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Truck>> GetByBranchIdAsync(int branchId)
    {
        return await _context.Trucks
            .Include(t => t.AssignedDriver)
            .Where(t => t.BranchId == branchId)
            .AsNoTracking()
            .OrderBy(t => t.TruckCode)
            .ToListAsync();
    }

    public async Task CreateAsync(Truck truck)
    {
        truck.TruckCode = truck.TruckCode.ToUpperInvariant();

        if (await _context.Trucks.AnyAsync(t => t.BranchId == truck.BranchId && t.TruckCode == truck.TruckCode))
        {
            throw new InvalidOperationException($"Truck code '{truck.TruckCode}' already exists in this branch.");
        }

        _context.Trucks.Add(truck);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Truck truck)
    {
        truck.TruckCode = truck.TruckCode.ToUpperInvariant();

        var existing = await _context.Trucks.FindAsync(truck.Id);
        if (existing == null) throw new KeyNotFoundException("Truck not found");

        if (existing.TruckCode != truck.TruckCode && await _context.Trucks.AnyAsync(t => t.BranchId == truck.BranchId && t.TruckCode == truck.TruckCode))
        {
            throw new InvalidOperationException($"Truck code '{truck.TruckCode}' already exists in this branch.");
        }

        existing.TruckCode = truck.TruckCode;
        existing.CapacityLbs = truck.CapacityLbs;
        existing.Plate = truck.Plate;
        existing.Description = truck.Description;
        existing.AssignedDriverUserId = truck.AssignedDriverUserId;
        existing.IsActive = truck.IsActive;

        await _context.SaveChangesAsync();
    }
}
