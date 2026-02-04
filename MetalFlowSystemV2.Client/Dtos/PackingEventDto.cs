using System;

namespace MetalFlowSystemV2.Shared.Dtos
{
    public class PackingEventDto
    {
        public int PackingEventId { get; set; }
        public int StationShiftId { get; set; }
        public int PickingListId { get; set; }
        public DateTime PackedAt { get; set; }
        public decimal PackedWeight { get; set; }
        public int LinesPacked { get; set; }
    }
}
