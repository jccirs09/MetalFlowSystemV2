using MetalFlowSystemV2.Client.Models;
using MetalFlowSystemV2.Data;
using MetalFlowSystemV2.Data.Entities;
using MetalFlowSystemV2.Data.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MetalFlowSystemV2.Endpoints
{
    public static class PickingListEndpoints
    {
        public static void MapPickingListEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/pickinglists").RequireAuthorization();

            group.MapGet("/", async (
                ClaimsPrincipal user,
                ApplicationDbContext context,
                [FromQuery] int branchId,
                [FromQuery] PickingListStatus? status) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                var query = context.PickingLists.Where(pl => pl.BranchId == branchId);
                if (status.HasValue)
                {
                    query = query.Where(pl => pl.Status == status.Value);
                }

                var list = await query
                    .OrderByDescending(pl => pl.CreatedAt)
                    .Select(pl => new PickingListSummaryDto
                    {
                        Id = pl.Id,
                        PickingListNumber = pl.PickingListNumber,
                        ShipDate = pl.ShipDate,
                        Buyer = pl.Buyer,
                        TotalWeightLbs = pl.TotalWeightLbs,
                        Status = pl.Status.ToString()
                    })
                    .ToListAsync();

                return Results.Ok(list);
            });

            group.MapGet("/{id}", async (
                int id,
                ClaimsPrincipal user,
                ApplicationDbContext context) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                // 1. Enforce Role Check
                if (!user.IsInRole("Admin") && !user.IsInRole("Supervisor") && !user.IsInRole("Planner"))
                {
                    return Results.Forbid();
                }

                var pl = await context.PickingLists
                    .Include(p => p.Lines)
                        .ThenInclude(l => l.ReservedMaterials)
                    .Include(p => p.Lines)
                        .ThenInclude(l => l.ProductionArea)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (pl == null) return Results.NotFound();

                // 2. Enforce Branch Access
                // Check if user is assigned to this branch via UserBranches or UserWorkAssignment
                var hasBranchAccess = await context.UserBranches
                    .AnyAsync(ub => ub.UserId == userId && ub.BranchId == pl.BranchId);

                if (!hasBranchAccess)
                {
                     // Fallback: Check active assignment (though usually covered by UserBranches if logic is consistent,
                     // but sometimes assignments exist without explicit UserBranch record? Unlikely but safer to check)
                     var hasActiveAssignment = await context.UserWorkAssignments
                        .AnyAsync(uwa => uwa.UserId == userId && uwa.BranchId == pl.BranchId && uwa.IsActive);

                     if (!hasActiveAssignment)
                     {
                         return Results.Forbid();
                     }
                }

                var dto = new PickingListDetailDto
                {
                    Id = pl.Id,
                    PickingListNumber = pl.PickingListNumber,
                    Buyer = pl.Buyer,
                    SalesRep = pl.SalesRep,
                    ShipTo = pl.ShipTo,
                    SoldTo = pl.SoldTo,
                    ShipVia = pl.ShipVia,
                    OrderInstructions = pl.OrderInstructions,
                    Status = pl.Status.ToString(),
                    Lines = pl.Lines.Select(l => new PickingListLineDto
                    {
                        Id = l.Id,
                        LineNumber = l.LineNumber,
                        ItemCode = l.ItemCode,
                        Description = l.Description,
                        OrderQuantity = l.OrderQuantity,
                        OrderUnit = l.OrderUnit,
                        WidthIn = l.WidthIn,
                        LengthIn = l.LengthIn,
                        LineWeightLbs = l.LineWeightLbs,
                        ProductionArea = l.ProductionArea?.Name ?? "Unknown",
                        LineInstructions = l.LineInstructions,
                        Status = l.LineStatus.ToString(),
                        ReservedMaterials = l.ReservedMaterials.Select(rm => new ReservedMaterialDto
                        {
                            TagNumber = rm.TagNumber,
                            Quantity = rm.Quantity,
                            Unit = rm.Unit,
                            Location = rm.Location
                        }).ToList()
                    }).OrderBy(l => l.LineNumber).ToList()
                };

                return Results.Ok(dto);
            });
        }
    }
}
