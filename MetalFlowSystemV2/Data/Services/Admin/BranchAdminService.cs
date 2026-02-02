using MetalFlowSystemV2.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalFlowSystemV2.Data.Services.Admin;

public class BranchAdminService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public BranchAdminService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Branch>> GetAllAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Branches
            .AsNoTracking()
            .OrderBy(b => b.Code)
            .ToListAsync();
    }

    public async Task<Branch?> GetByIdAsync(int id)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Branches.FindAsync(id);
    }

    public async Task CreateAsync(Branch branch)
    {
        using var context = _contextFactory.CreateDbContext();
        branch.Code = branch.Code.ToUpperInvariant();

        if (await context.Branches.AnyAsync(b => b.Code == branch.Code))
        {
            throw new InvalidOperationException($"Branch code '{branch.Code}' already exists.");
        }

        context.Branches.Add(branch);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Branch branch)
    {
        using var context = _contextFactory.CreateDbContext();
        branch.Code = branch.Code.ToUpperInvariant();

        var existing = await context.Branches.FindAsync(branch.Id);
        if (existing == null) throw new KeyNotFoundException("Branch not found");

        if (existing.Code != branch.Code && await context.Branches.AnyAsync(b => b.Code == branch.Code))
        {
            throw new InvalidOperationException($"Branch code '{branch.Code}' already exists.");
        }

        // Update fields
        existing.Code = branch.Code;
        existing.Name = branch.Name;
        existing.City = branch.City;
        existing.Region = branch.Region;
        existing.Country = branch.Country;
        existing.IsActive = branch.IsActive;

        await context.SaveChangesAsync();
    }
}
