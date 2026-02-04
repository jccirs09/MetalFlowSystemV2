using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MetalFlowSystemV2.Data.Services;
using MetalFlowSystemV2.Client.Models;
using Microsoft.EntityFrameworkCore;
using MetalFlowSystemV2.Data;
using MetalFlowSystemV2.Data.Entities;

namespace MetalFlowSystemV2.Api
{
    public static class PickingListEndpoints
    {
        public static void MapPickingListEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/pickinglists").RequireAuthorization();

            group.MapGet("/", async (
                HttpContext httpContext,
                ApplicationDbContext db,
                string? status) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null) return Results.Unauthorized();

                var userBranch = await db.UserBranches.FirstOrDefaultAsync(ub => ub.UserId == userId && ub.IsDefault);
                if (userBranch == null) return Results.NotFound("No default branch.");

                var query = db.PickingLists
                    .Where(p => p.BranchId == userBranch.BranchId);

                if (!string.IsNullOrEmpty(status))
                {
                    if (Enum.TryParse<PickingListStatus>(status.Replace(" ", ""), true, out var statusEnum))
                    {
                         query = query.Where(p => p.Status == statusEnum);
                    }
                }

                var lists = await query
                    .OrderBy(p => p.ShipDate)
                    .Select(p => new PickingListDto
                    {
                        Id = p.Id,
                        BranchId = p.BranchId,
                        PickingListNumber = p.PickingListNumber,
                        // ShipDate is DateTime?, need DateOnly conversion
                        ShipDate = p.ShipDate.HasValue ? DateOnly.FromDateTime(p.ShipDate.Value) : null,
                        Buyer = p.Buyer,
                        TotalWeight = p.TotalWeightLbs,
                        Status = p.Status.ToString()
                    })
                    .ToListAsync();

                return Results.Ok(lists);
            });

            group.MapGet("/{id}", async (
                int id,
                HttpContext httpContext,
                ApplicationDbContext db) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null) return Results.Unauthorized();

                var userBranch = await db.UserBranches.FirstOrDefaultAsync(ub => ub.UserId == userId && ub.IsDefault);
                if (userBranch == null) return Results.NotFound("No default branch.");

                var pl = await db.PickingLists
                    .Include(p => p.Lines)
                        .ThenInclude(l => l.ReservedMaterials)
                    .Include(p => p.Lines)
                        .ThenInclude(l => l.ProductionArea)
                    .FirstOrDefaultAsync(p => p.Id == id && p.BranchId == userBranch.BranchId);

                if (pl == null) return Results.NotFound();

                var dto = new PickingListDetailDto
                {
                    Id = pl.Id,
                    BranchId = pl.BranchId,
                    PickingListNumber = pl.PickingListNumber,
                    ShipDate = pl.ShipDate.HasValue ? DateOnly.FromDateTime(pl.ShipDate.Value) : null,
                    Buyer = pl.Buyer,
                    Status = pl.Status.ToString(),
                    TotalWeight = pl.TotalWeightLbs,
                    SalesRep = pl.SalesRep,
                    ShipToName = pl.ShipTo, // Assuming ShipTo holds the name or part of it. The property is called ShipTo string.
                    ShipToAddress1 = pl.ShipTo, // Mapping generic text to specific fields for now
                    ShipToCity = "",
                    ShipToState = "",
                    ShipToZip = "",
                    ShipVia = pl.ShipVia,
                    Instructions = pl.OrderInstructions,
                    Lines = pl.Lines.Select(l => new PickingListLineDto
                    {
                        Id = l.Id,
                        LineNumber = l.LineNumber,
                        ItemCode = l.ItemCode,
                        Description = l.Description,
                        OrderedQty = l.OrderQuantity,
                        OrderedUnit = l.OrderUnit,
                        Width = l.WidthIn,
                        Length = l.LengthIn,
                        Weight = l.LineWeightLbs ?? 0,
                        ProductionArea = l.ProductionArea?.Name ?? "",
                        Notes = l.LineInstructions,
                        ReservedMaterials = l.ReservedMaterials.Select(rm => $"{rm.TagNumber} ({rm.Quantity} {rm.Unit})").ToList()
                    }).OrderBy(l => l.LineNumber).ToList()
                };

                return Results.Ok(dto);
            });
        }
    }
}
