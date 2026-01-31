using MetalFlowSystemV2.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalFlowSystemV2.Data.Services.Admin;

public class BranchAdminService
{
    private readonly ApplicationDbContext _context;

    public BranchAdminService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Branch>> GetAllAsync()
    {
        return await _context.Branches
            .AsNoTracking()
            .OrderBy(b => b.Code)
            .ToListAsync();
    }

    public async Task<Branch?> GetByIdAsync(int id)
    {
        return await _context.Branches.FindAsync(id);
    }

    public async Task CreateAsync(Branch branch)
    {
        branch.Code = branch.Code.ToUpperInvariant();

        if (await _context.Branches.AnyAsync(b => b.Code == branch.Code))
        {
            throw new InvalidOperationException($"Branch code '{branch.Code}' already exists.");
        }

        _context.Branches.Add(branch);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Branch branch)
    {
        branch.Code = branch.Code.ToUpperInvariant();

        var existing = await _context.Branches.FindAsync(branch.Id);
        if (existing == null) throw new KeyNotFoundException("Branch not found");

        if (existing.Code != branch.Code && await _context.Branches.AnyAsync(b => b.Code == branch.Code))
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

        await _context.SaveChangesAsync();
    }
}
