namespace MetalFlowSystemV2.Client.Models
{
    public class PickingListSummaryDto
    {
        public int Id { get; set; }
        public string PickingListNumber { get; set; } = string.Empty;
        public DateTime? ShipDate { get; set; }
        public string Buyer { get; set; } = string.Empty;
        public decimal TotalWeightLbs { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class PickingListDetailDto
    {
        public int Id { get; set; }
        public string PickingListNumber { get; set; } = string.Empty;
        public string Buyer { get; set; } = string.Empty;
        public string SalesRep { get; set; } = string.Empty;
        public string ShipTo { get; set; } = string.Empty;
        public string SoldTo { get; set; } = string.Empty;
        public string ShipVia { get; set; } = string.Empty;
        public string OrderInstructions { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<PickingListLineDto> Lines { get; set; } = new();
    }

    public class PickingListLineDto
    {
        public int Id { get; set; }
        public int LineNumber { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal OrderQuantity { get; set; }
        public string OrderUnit { get; set; } = string.Empty;
        public decimal WidthIn { get; set; }
        public decimal LengthIn { get; set; }
        public decimal? LineWeightLbs { get; set; }
        public string ProductionArea { get; set; } = string.Empty;
        public string LineInstructions { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<ReservedMaterialDto> ReservedMaterials { get; set; } = new();
    }

    public class ReservedMaterialDto
    {
        public string TagNumber { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }
}
