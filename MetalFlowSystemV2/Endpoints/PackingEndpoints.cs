using MetalFlowSystemV2.Data;
using MetalFlowSystemV2.Data.Entities;
using MetalFlowSystemV2.Shared.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MetalFlowSystemV2.Endpoints
{
    public static class PackingEndpoints
    {
        public static void MapPackingEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/packing").RequireAuthorization();

            group.MapPost("/", async (PackingEventDto dto, ApplicationDbContext db, ClaimsPrincipal user) =>
            {
                var evt = new PackingEvent
                {
                    StationShiftId = dto.StationShiftId,
                    PickingListId = dto.PickingListId,
                    PackedAt = DateTime.UtcNow,
                    PackedWeight = dto.PackedWeight,
                    LinesPacked = dto.LinesPacked
                };
                db.PackingEvents.Add(evt);
                await db.SaveChangesAsync();
                return Results.Ok(evt.PackingEventId);
            });

            group.MapGet("/shift/{stationShiftId}", async (int stationShiftId, ApplicationDbContext db) =>
            {
                var events = await db.PackingEvents
                    .Where(e => e.StationShiftId == stationShiftId)
                    .Select(e => new PackingEventDto
                    {
                        PackingEventId = e.PackingEventId,
                        StationShiftId = e.StationShiftId,
                        PickingListId = e.PickingListId,
                        PackedAt = e.PackedAt,
                        PackedWeight = e.PackedWeight,
                        LinesPacked = e.LinesPacked
                    })
                    .ToListAsync();
                return Results.Ok(events);
            });
        }
    }
}
