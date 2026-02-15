using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MetalFlowSystemV2.Data.Services;
using MetalFlowSystemV2.Client.Models;
using Microsoft.EntityFrameworkCore;
using MetalFlowSystemV2.Data;

namespace MetalFlowSystemV2.Api
{
    public static class ShiftEndpoints
    {
        public static void MapShiftEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/shifts").RequireAuthorization();

            group.MapGet("/assignment", async (
                HttpContext httpContext,
                ShiftInstanceService shiftService,
                ApplicationDbContext db) => // Injecting DbContext directly for quick lookup or via service
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null) return Results.Unauthorized();

                // Get User Default Branch
                // We need to resolve the branch. The Admin usually sets a default branch.
                // Or we look at claims.
                // Let's look up the user's default branch.
                var userBranch = await db.UserBranches
                    .Include(ub => ub.Branch)
                    .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.IsDefault);

                if (userBranch == null)
                    return Results.NotFound("No default branch assigned.");

                var assignment = await shiftService.ResolveActiveAssignmentAsync(userId, userBranch.BranchId);

                if (assignment == null)
                    return Results.NotFound("No active work assignment found.");

                // Map to DTO
                var dto = new UserAssignmentDto
                {
                    UserId = userId,
                    BranchId = userBranch.BranchId,
                    BranchName = userBranch.Branch?.Name ?? "",
                    WorkMode = assignment.WorkMode.ToString(),
                    ProductionAreaId = assignment.ProductionAreaId,
                    ProductionAreaName = assignment.ProductionArea?.Name,
                    PackingStationId = assignment.PackingStationId,
                    PackingStationName = assignment.PackingStation?.Name,
                    ShiftTemplateId = assignment.ShiftTemplateId,
                    ShiftName = assignment.ShiftTemplate?.Name ?? "",
                    StartTime = assignment.ShiftTemplate?.StartTime.ToTimeSpan() ?? TimeSpan.Zero,
                    EndTime = assignment.ShiftTemplate?.EndTime.ToTimeSpan() ?? TimeSpan.Zero
                };

                // Check Attendance
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var attendance = await db.ShiftAttendances
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.ShiftDate == today && a.ShiftTemplateId == assignment.ShiftTemplateId);

                if (attendance != null)
                {
                    dto.IsCheckedIn = true;
                    dto.CheckedInAt = attendance.CheckedInAt;
                }

                // Resolve StationShiftId if assigned to PackingStation
                if (assignment.PackingStationId.HasValue)
                {
                    var stationShift = await db.StationShifts
                        .FirstOrDefaultAsync(ss =>
                            ss.BranchId == userBranch.BranchId &&
                            ss.PackingStationId == assignment.PackingStationId.Value &&
                            ss.ShiftDate == today &&
                            ss.ShiftTemplateId == assignment.ShiftTemplateId);

                    if (stationShift != null)
                    {
                        dto.StationShiftId = stationShift.Id;
                    }
                }

                return Results.Ok(dto);
            });

            group.MapPost("/checkin", async (
                HttpContext httpContext,
                ShiftInstanceService shiftService,
                ApplicationDbContext db) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null) return Results.Unauthorized();

                var userBranch = await db.UserBranches
                    .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.IsDefault);

                if (userBranch == null)
                    return Results.NotFound("No default branch assigned.");

                try
                {
                    var today = DateOnly.FromDateTime(DateTime.UtcNow);
                    await shiftService.CheckInAsync(userId, userBranch.BranchId, today);
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            });
        }
    }
}
