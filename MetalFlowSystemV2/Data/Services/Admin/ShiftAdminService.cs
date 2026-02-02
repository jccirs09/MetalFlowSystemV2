using MetalFlowSystemV2.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalFlowSystemV2.Data.Services.Admin;

public class ShiftAdminService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public ShiftAdminService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Shift>> GetByBranchIdAsync(int branchId)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Shifts
            .Where(s => s.BranchId == branchId)
            .AsNoTracking()
            .OrderBy(s => s.Code)
            .ToListAsync();
    }

    public async Task CreateAsync(Shift shift)
    {
        using var context = _contextFactory.CreateDbContext();
        shift.Code = shift.Code.ToUpperInvariant();

        if (await context.Shifts.AnyAsync(s => s.BranchId == shift.BranchId && s.Code == shift.Code))
        {
            throw new InvalidOperationException($"Shift code '{shift.Code}' already exists in this branch.");
        }

        context.Shifts.Add(shift);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Shift shift)
    {
        using var context = _contextFactory.CreateDbContext();
        shift.Code = shift.Code.ToUpperInvariant();

        var existing = await context.Shifts.FindAsync(shift.Id);
        if (existing == null) throw new KeyNotFoundException("Shift not found");

        if (existing.Code != shift.Code && await context.Shifts.AnyAsync(s => s.BranchId == shift.BranchId && s.Code == shift.Code))
        {
            throw new InvalidOperationException($"Shift code '{shift.Code}' already exists in this branch.");
        }

        existing.Code = shift.Code;
        existing.Name = shift.Name;
        existing.StartTime = shift.StartTime;
        existing.EndTime = shift.EndTime;
        existing.CrossesMidnight = shift.CrossesMidnight;
        existing.IsActive = shift.IsActive;

        await context.SaveChangesAsync();
    }
}
