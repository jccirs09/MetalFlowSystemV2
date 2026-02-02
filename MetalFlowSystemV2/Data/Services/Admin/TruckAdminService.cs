using MetalFlowSystemV2.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalFlowSystemV2.Data.Services.Admin;

public class TruckAdminService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public TruckAdminService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Truck>> GetByBranchIdAsync(int branchId)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Trucks
            .Include(t => t.AssignedDriver)
            .Where(t => t.BranchId == branchId)
            .AsNoTracking()
            .OrderBy(t => t.TruckCode)
            .ToListAsync();
    }

    public async Task CreateAsync(Truck truck)
    {
        using var context = _contextFactory.CreateDbContext();
        truck.TruckCode = truck.TruckCode.ToUpperInvariant();

        if (await context.Trucks.AnyAsync(t => t.BranchId == truck.BranchId && t.TruckCode == truck.TruckCode))
        {
            throw new InvalidOperationException($"Truck code '{truck.TruckCode}' already exists in this branch.");
        }

        context.Trucks.Add(truck);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Truck truck)
    {
        using var context = _contextFactory.CreateDbContext();
        truck.TruckCode = truck.TruckCode.ToUpperInvariant();

        var existing = await context.Trucks.FindAsync(truck.Id);
        if (existing == null) throw new KeyNotFoundException("Truck not found");

        if (existing.TruckCode != truck.TruckCode && await context.Trucks.AnyAsync(t => t.BranchId == truck.BranchId && t.TruckCode == truck.TruckCode))
        {
            throw new InvalidOperationException($"Truck code '{truck.TruckCode}' already exists in this branch.");
        }

        existing.TruckCode = truck.TruckCode;
        existing.CapacityLbs = truck.CapacityLbs;
        existing.Plate = truck.Plate;
        existing.Description = truck.Description;
        existing.AssignedDriverUserId = truck.AssignedDriverUserId;
        existing.IsActive = truck.IsActive;

        await context.SaveChangesAsync();
    }
}
