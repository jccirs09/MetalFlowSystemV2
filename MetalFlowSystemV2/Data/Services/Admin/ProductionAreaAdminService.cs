using MetalFlowSystemV2.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalFlowSystemV2.Data.Services.Admin;

public class ProductionAreaAdminService
{
    private readonly ApplicationDbContext _context;

    public ProductionAreaAdminService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductionArea>> GetByBranchIdAsync(int branchId)
    {
        return await _context.ProductionAreas
            .Where(p => p.BranchId == branchId)
            .AsNoTracking()
            .OrderBy(p => p.Code)
            .ToListAsync();
    }

    public async Task CreateAsync(ProductionArea area)
    {
        area.Code = area.Code.ToUpperInvariant();

        if (await _context.ProductionAreas.AnyAsync(p => p.BranchId == area.BranchId && p.Code == area.Code))
        {
            throw new InvalidOperationException($"Production Area code '{area.Code}' already exists in this branch.");
        }

        _context.ProductionAreas.Add(area);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProductionArea area)
    {
        area.Code = area.Code.ToUpperInvariant();

        var existing = await _context.ProductionAreas.FindAsync(area.Id);
        if (existing == null) throw new KeyNotFoundException("Production Area not found");

        if (existing.Code != area.Code && await _context.ProductionAreas.AnyAsync(p => p.BranchId == area.BranchId && p.Code == area.Code))
        {
            throw new InvalidOperationException($"Production Area code '{area.Code}' already exists in this branch.");
        }

        existing.Code = area.Code;
        existing.Name = area.Name;
        existing.AreaType = area.AreaType;
        existing.IsActive = area.IsActive;
        // BranchId should typically not change, or if it does, check existence there too.
        // For now assuming BranchId is fixed or consistent.

        await _context.SaveChangesAsync();
    }
}
