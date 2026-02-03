using MetalFlowSystemV2.Client.Models;
using MetalFlowSystemV2.Data;
using MetalFlowSystemV2.Data.Services;
using MetalFlowSystemV2.Data.Services.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MetalFlowSystemV2.Endpoints
{
    public static class ShiftEndpoints
    {
        public static void MapShiftEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/shifts").RequireAuthorization();

            group.MapGet("/my-assignment", async (
                ClaimsPrincipal user,
                UserWorkAssignmentService assignmentService,
                ApplicationDbContext context) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                // Resolve Branch
                // 1. Active Assignment (Already Checked In logic? No, Assignment exists regardless of CheckIn)
                // We check if user has an active assignment record.
                var activeAssignment = await context.UserWorkAssignments
                    .Include(a => a.Branch)
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.IsActive);

                int? branchId = activeAssignment?.BranchId;

                if (branchId == null)
                {
                    // 2. Default Branch
                    var userBranch = await context.UserBranches
                        .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.IsDefault);
                    branchId = userBranch?.BranchId;
                }

                if (branchId == null)
                {
                    // 3. Single Branch
                    var branches = await context.UserBranches.Where(ub => ub.UserId == userId).ToListAsync();
                    if (branches.Count == 1) branchId = branches[0].BranchId;
                }

                if (branchId == null) return Results.NotFound("No default branch found. Please ask an admin to assign you to a branch or set a default.");

                // Get Assignment details
                var assignment = await assignmentService.GetActiveAssignmentAsync(userId, branchId.Value);

                if (assignment == null)
                {
                    // User resolved to a branch but has no active assignment?
                    // Return minimal info so they know they are not assigned.
                    var branch = await context.Branches.FindAsync(branchId.Value);
                    return Results.Ok(new UserAssignmentDto
                    {
                        BranchId = branchId.Value,
                        BranchName = branch?.Name ?? "Unknown",
                        Message = "No active assignment found."
                    });
                }

                // Check Attendance
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var attendance = await context.ShiftAttendances
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.BranchId == branchId.Value && a.ShiftDate == today && a.ShiftTemplateId == assignment.ShiftTemplateId);

                return Results.Ok(new UserAssignmentDto
                {
                    BranchId = branchId.Value,
                    BranchName = assignment.Branch?.Name ?? "",
                    ShiftName = assignment.ShiftTemplate?.Name ?? "",
                    StartTime = assignment.ShiftTemplate?.StartTime.ToTimeSpan() ?? TimeSpan.Zero,
                    EndTime = assignment.ShiftTemplate?.EndTime.ToTimeSpan() ?? TimeSpan.Zero,
                    WorkMode = assignment.WorkMode.ToString(),
                    LocationName = assignment.WorkMode == MetalFlowSystemV2.Data.Entities.WorkMode.ProductionArea
                        ? assignment.ProductionArea?.Name ?? "Unknown"
                        : assignment.PackingStation?.Name ?? "Unknown",
                    IsCheckedIn = attendance != null,
                    CheckedInAt = attendance?.CheckedInAt
                });
            });

            group.MapPost("/check-in", async (
                ClaimsPrincipal user,
                ShiftInstanceService shiftService,
                [FromBody] CheckInRequest request) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                try
                {
                    var today = DateOnly.FromDateTime(DateTime.UtcNow);
                    await shiftService.CheckInAsync(userId, request.BranchId, today);
                    return Results.Ok(new CheckInResult { Success = true, Message = "Checked in successfully." });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new CheckInResult { Success = false, Message = ex.Message });
                }
            });
        }
    }
}
