using MetalFlowSystemV2.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MetalFlowSystemV2.Data.Services.Admin;

public class ShiftAdminService
{
    private readonly ApplicationDbContext _context;

    public ShiftAdminService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Shift>> GetByBranchIdAsync(int branchId)
    {
        return await _context.Shifts
            .Where(s => s.BranchId == branchId)
            .AsNoTracking()
            .OrderBy(s => s.Code)
            .ToListAsync();
    }

    public async Task CreateAsync(Shift shift)
    {
        shift.Code = shift.Code.ToUpperInvariant();

        if (await _context.Shifts.AnyAsync(s => s.BranchId == shift.BranchId && s.Code == shift.Code))
        {
            throw new InvalidOperationException($"Shift code '{shift.Code}' already exists in this branch.");
        }

        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Shift shift)
    {
        shift.Code = shift.Code.ToUpperInvariant();

        var existing = await _context.Shifts.FindAsync(shift.Id);
        if (existing == null) throw new KeyNotFoundException("Shift not found");

        if (existing.Code != shift.Code && await _context.Shifts.AnyAsync(s => s.BranchId == shift.BranchId && s.Code == shift.Code))
        {
            throw new InvalidOperationException($"Shift code '{shift.Code}' already exists in this branch.");
        }

        existing.Code = shift.Code;
        existing.Name = shift.Name;
        existing.StartTime = shift.StartTime;
        existing.EndTime = shift.EndTime;
        existing.CrossesMidnight = shift.CrossesMidnight;
        existing.IsActive = shift.IsActive;

        await _context.SaveChangesAsync();
    }
}
