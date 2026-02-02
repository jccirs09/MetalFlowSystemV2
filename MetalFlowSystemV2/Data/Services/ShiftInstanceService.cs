using Microsoft.EntityFrameworkCore;
using MetalFlowSystemV2.Data.Entities;

namespace MetalFlowSystemV2.Data.Services
{
    public class ShiftInstanceService
    {
        private readonly ApplicationDbContext _context;
        private readonly Admin.UserWorkAssignmentService _assignmentService;

        public ShiftInstanceService(ApplicationDbContext context, Admin.UserWorkAssignmentService assignmentService)
        {
            _context = context;
            _assignmentService = assignmentService;
        }

        public async Task<UserWorkAssignment?> ResolveActiveAssignmentAsync(string userId, int branchId)
        {
            return await _assignmentService.GetActiveAssignmentAsync(userId, branchId);
        }

        public async Task CheckInAsync(string userId, int branchId, DateOnly shiftDate)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Resolve Assignment
                var assignment = await ResolveActiveAssignmentAsync(userId, branchId);
                if (assignment == null)
                    throw new InvalidOperationException("No active work assignment found for this user.");

                int? areaShiftId = null;
                int? stationShiftId = null;

                // 2. Get/Create Shift Instance
                if (assignment.WorkMode == WorkMode.ProductionArea)
                {
                    var areaShift = await GetOrCreateAreaShiftAsync(branchId, assignment.ProductionAreaId!.Value, assignment.ShiftTemplateId, shiftDate);
                    if (areaShift.Status == ShiftStatus.Closed)
                        throw new InvalidOperationException("This shift is closed.");
                    areaShiftId = areaShift.Id;
                }
                else
                {
                    var stationShift = await GetOrCreateStationShiftAsync(branchId, assignment.PackingStationId!.Value, assignment.ShiftTemplateId, shiftDate);
                    if (stationShift.Status == ShiftStatus.Closed)
                        throw new InvalidOperationException("This shift is closed.");
                    stationShiftId = stationShift.Id;
                }

                // 3. Create Attendance (Idempotent)
                var existingAttendance = await _context.ShiftAttendances
                    .FirstOrDefaultAsync(a =>
                        a.UserId == userId &&
                        a.BranchId == branchId &&
                        a.ShiftDate == shiftDate &&
                        a.ShiftTemplateId == assignment.ShiftTemplateId &&
                        a.WorkMode == assignment.WorkMode &&
                        a.ProductionAreaId == assignment.ProductionAreaId &&
                        a.PackingStationId == assignment.PackingStationId);

                if (existingAttendance == null)
                {
                    var attendance = new ShiftAttendance
                    {
                        BranchId = branchId,
                        ShiftDate = shiftDate,
                        ShiftTemplateId = assignment.ShiftTemplateId,
                        UserId = userId,
                        WorkMode = assignment.WorkMode,
                        ProductionAreaId = assignment.ProductionAreaId,
                        PackingStationId = assignment.PackingStationId,
                        AreaShiftId = areaShiftId,
                        StationShiftId = stationShiftId,
                        CheckedInAt = DateTime.UtcNow
                    };
                    _context.ShiftAttendances.Add(attendance);
                    await _context.SaveChangesAsync();

                    // 4. Update Headcount
                    await UpdateHeadcountsAsync(areaShiftId, stationShiftId);
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<AreaShift> GetOrCreateAreaShiftAsync(int branchId, int areaId, int shiftTemplateId, DateOnly date)
        {
            var shift = await _context.AreaShifts
                .FirstOrDefaultAsync(s => s.BranchId == branchId && s.ProductionAreaId == areaId && s.ShiftTemplateId == shiftTemplateId && s.ShiftDate == date);

            if (shift == null)
            {
                shift = new AreaShift
                {
                    BranchId = branchId,
                    ProductionAreaId = areaId,
                    ShiftTemplateId = shiftTemplateId,
                    ShiftDate = date,
                    Status = ShiftStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                _context.AreaShifts.Add(shift);
                await _context.SaveChangesAsync();
            }
            return shift;
        }

        private async Task<StationShift> GetOrCreateStationShiftAsync(int branchId, int stationId, int shiftTemplateId, DateOnly date)
        {
            var shift = await _context.StationShifts
                .FirstOrDefaultAsync(s => s.BranchId == branchId && s.PackingStationId == stationId && s.ShiftTemplateId == shiftTemplateId && s.ShiftDate == date);

            if (shift == null)
            {
                shift = new StationShift
                {
                    BranchId = branchId,
                    PackingStationId = stationId,
                    ShiftTemplateId = shiftTemplateId,
                    ShiftDate = date,
                    Status = ShiftStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                _context.StationShifts.Add(shift);
                await _context.SaveChangesAsync();
            }
            return shift;
        }

        private async Task UpdateHeadcountsAsync(int? areaShiftId, int? stationShiftId)
        {
            if (areaShiftId.HasValue)
            {
                var shift = await _context.AreaShifts.FindAsync(areaShiftId.Value);
                if (shift != null)
                {
                    // Calculate Expected: Count of active assignments for this context
                    var expected = await _context.UserWorkAssignments
                        .CountAsync(a => a.BranchId == shift.BranchId &&
                                         a.ProductionAreaId == shift.ProductionAreaId &&
                                         a.ShiftTemplateId == shift.ShiftTemplateId &&
                                         a.IsActive);

                    // Calculate Confirmed: Count of attendance
                    var confirmed = await _context.ShiftAttendances
                        .CountAsync(a => a.AreaShiftId == areaShiftId.Value);

                    shift.ExpectedHeadcount = expected;
                    shift.ConfirmedHeadcount = confirmed; // Unless manually overridden logic is added later
                    await _context.SaveChangesAsync();
                }
            }

            if (stationShiftId.HasValue)
            {
                var shift = await _context.StationShifts.FindAsync(stationShiftId.Value);
                if (shift != null)
                {
                    var expected = await _context.UserWorkAssignments
                        .CountAsync(a => a.BranchId == shift.BranchId &&
                                         a.PackingStationId == shift.PackingStationId &&
                                         a.ShiftTemplateId == shift.ShiftTemplateId &&
                                         a.IsActive);

                    var confirmed = await _context.ShiftAttendances
                        .CountAsync(a => a.StationShiftId == stationShiftId.Value);

                    shift.ExpectedHeadcount = expected;
                    shift.ConfirmedHeadcount = confirmed;
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task CloseShiftAsync(int shiftInstanceId, WorkMode mode, string userId)
        {
            if (mode == WorkMode.ProductionArea)
            {
                var shift = await _context.AreaShifts.FindAsync(shiftInstanceId);
                if (shift != null)
                {
                    shift.Status = ShiftStatus.Closed;
                    shift.ClosedAt = DateTime.UtcNow;
                    shift.ClosedByUserId = userId;
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var shift = await _context.StationShifts.FindAsync(shiftInstanceId);
                if (shift != null)
                {
                    shift.Status = ShiftStatus.Closed;
                    shift.ClosedAt = DateTime.UtcNow;
                    shift.ClosedByUserId = userId;
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task OverrideHeadcountAsync(int shiftInstanceId, WorkMode mode, int newHeadcount, string reason)
        {
             if (mode == WorkMode.ProductionArea)
            {
                var shift = await _context.AreaShifts.FindAsync(shiftInstanceId);
                if (shift != null)
                {
                    shift.ConfirmedHeadcount = newHeadcount;
                    // Ideally log this override in an audit table or specific attendance record
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var shift = await _context.StationShifts.FindAsync(shiftInstanceId);
                if (shift != null)
                {
                    shift.ConfirmedHeadcount = newHeadcount;
                    await _context.SaveChangesAsync();
                }
            }
        }

        // Supervisor Queries
        public async Task<List<AreaShift>> GetAreaShiftsAsync(int branchId, DateOnly? date = null)
        {
            var query = _context.AreaShifts
                .Include(s => s.ProductionArea)
                .Include(s => s.ShiftTemplate)
                .Where(s => s.BranchId == branchId);

            if (date.HasValue)
                query = query.Where(s => s.ShiftDate == date.Value);

            return await query.OrderByDescending(s => s.ShiftDate).ToListAsync();
        }

        public async Task<List<StationShift>> GetStationShiftsAsync(int branchId, DateOnly? date = null)
        {
            var query = _context.StationShifts
                .Include(s => s.PackingStation)
                .Include(s => s.ShiftTemplate)
                .Where(s => s.BranchId == branchId);

            if (date.HasValue)
                query = query.Where(s => s.ShiftDate == date.Value);

            return await query.OrderByDescending(s => s.ShiftDate).ToListAsync();
        }
    }
}
