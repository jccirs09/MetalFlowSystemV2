using Microsoft.EntityFrameworkCore;
using MetalFlowSystemV2.Data.Entities;

namespace MetalFlowSystemV2.Data.Services.Admin
{
    public class UserWorkAssignmentService
    {
        private readonly ApplicationDbContext _context;

        public UserWorkAssignmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserWorkAssignment>> GetByBranchAsync(int branchId)
        {
            return await _context.UserWorkAssignments
                .Include(a => a.User)
                .Include(a => a.ShiftTemplate)
                .Include(a => a.ProductionArea)
                .Include(a => a.PackingStation)
                .Where(a => a.BranchId == branchId)
                .OrderBy(a => a.User!.UserName)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<UserWorkAssignment?> GetActiveAssignmentAsync(string userId, int branchId)
        {
            return await _context.UserWorkAssignments
                .Include(a => a.ShiftTemplate)
                .Include(a => a.ProductionArea)
                .Include(a => a.PackingStation)
                .FirstOrDefaultAsync(a => a.UserId == userId && a.BranchId == branchId && a.IsActive);
        }

        public async Task CreateAsync(UserWorkAssignment assignment)
        {
            await ValidateAssignmentAsync(assignment);

            // Deactivate any existing active assignment for this user/branch
            var existing = await GetActiveAssignmentAsync(assignment.UserId, assignment.BranchId);
            if (existing != null)
            {
                existing.IsActive = false;
                _context.UserWorkAssignments.Update(existing);
            }

            assignment.CreatedAt = DateTime.UtcNow;
            assignment.UpdatedAt = DateTime.UtcNow;
            assignment.IsActive = true;

            _context.UserWorkAssignments.Add(assignment);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(UserWorkAssignment assignment)
        {
            await ValidateAssignmentAsync(assignment);

            var existing = await _context.UserWorkAssignments.FindAsync(assignment.Id);
            if (existing == null) throw new KeyNotFoundException("Assignment not found");

            existing.ShiftTemplateId = assignment.ShiftTemplateId;
            existing.WorkMode = assignment.WorkMode;
            existing.ProductionAreaId = assignment.ProductionAreaId;
            existing.PackingStationId = assignment.PackingStationId;
            existing.Role = assignment.Role;
            existing.IsActive = assignment.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task DeactivateAsync(int id)
        {
            var assignment = await _context.UserWorkAssignments.FindAsync(id);
            if (assignment != null)
            {
                assignment.IsActive = false;
                assignment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        private async Task ValidateAssignmentAsync(UserWorkAssignment assignment)
        {
            // XOR Check
            if (assignment.WorkMode == WorkMode.ProductionArea)
            {
                if (assignment.ProductionAreaId == null || assignment.PackingStationId != null)
                    throw new InvalidOperationException("Production Area must be selected for Production Mode.");

                // Verify Branch Match
                var area = await _context.ProductionAreas.FindAsync(assignment.ProductionAreaId);
                if (area == null || area.BranchId != assignment.BranchId)
                    throw new InvalidOperationException("Invalid Production Area for this branch.");
            }
            else if (assignment.WorkMode == WorkMode.PackingStation)
            {
                if (assignment.PackingStationId == null || assignment.ProductionAreaId != null)
                    throw new InvalidOperationException("Packing Station must be selected for Packing Mode.");

                var station = await _context.PackingStations.FindAsync(assignment.PackingStationId);
                if (station == null || station.BranchId != assignment.BranchId)
                    throw new InvalidOperationException("Invalid Packing Station for this branch.");
            }

            // Verify Shift
            var shift = await _context.Shifts.FindAsync(assignment.ShiftTemplateId);
            if (shift == null || shift.BranchId != assignment.BranchId || !shift.IsActive)
                throw new InvalidOperationException("Invalid or inactive Shift Template.");
        }
    }
}
