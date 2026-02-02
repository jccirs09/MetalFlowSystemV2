using MetalFlowSystemV2.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalFlowSystemV2.Data.Services.Admin;

public class ProductionAreaAdminService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public ProductionAreaAdminService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<ProductionArea>> GetByBranchIdAsync(int branchId)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.ProductionAreas
            .Where(p => p.BranchId == branchId)
            .AsNoTracking()
            .OrderBy(p => p.Code)
            .ToListAsync();
    }

    public async Task CreateAsync(ProductionArea area)
    {
        using var context = _contextFactory.CreateDbContext();
        area.Code = area.Code.ToUpperInvariant();

        if (await context.ProductionAreas.AnyAsync(p => p.BranchId == area.BranchId && p.Code == area.Code))
        {
            throw new InvalidOperationException($"Production Area code '{area.Code}' already exists in this branch.");
        }

        context.ProductionAreas.Add(area);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProductionArea area)
    {
        using var context = _contextFactory.CreateDbContext();
        area.Code = area.Code.ToUpperInvariant();

        var existing = await context.ProductionAreas.FindAsync(area.Id);
        if (existing == null) throw new KeyNotFoundException("Production Area not found");

        if (existing.Code != area.Code && await context.ProductionAreas.AnyAsync(p => p.BranchId == area.BranchId && p.Code == area.Code))
        {
            throw new InvalidOperationException($"Production Area code '{area.Code}' already exists in this branch.");
        }

        existing.Code = area.Code;
        existing.Name = area.Name;
        existing.AreaType = area.AreaType;
        existing.IsActive = area.IsActive;
        // BranchId should typically not change, or if it does, check existence there too.
        // For now assuming BranchId is fixed or consistent.

        await context.SaveChangesAsync();
    }
}
