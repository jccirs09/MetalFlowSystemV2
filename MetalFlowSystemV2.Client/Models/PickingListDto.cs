namespace MetalFlowSystemV2.Client.Models
{
    public class PickingListDto
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public string PickingListNumber { get; set; } = string.Empty;
        public DateOnly? ShipDate { get; set; }
        public string Buyer { get; set; } = string.Empty;
        public decimal TotalWeight { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class PickingListDetailDto : PickingListDto
    {
        public string SalesRep { get; set; } = string.Empty;
        public string ShipToName { get; set; } = string.Empty;
        public string ShipToAddress1 { get; set; } = string.Empty;
        public string ShipToCity { get; set; } = string.Empty;
        public string ShipToState { get; set; } = string.Empty;
        public string ShipToZip { get; set; } = string.Empty;
        public string ShipVia { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;

        public List<PickingListLineDto> Lines { get; set; } = new();
    }

    public class PickingListLineDto
    {
        public int Id { get; set; }
        public int LineNumber { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal OrderedQty { get; set; }
        public string OrderedUnit { get; set; } = string.Empty;
        public decimal Width { get; set; }
        public decimal Length { get; set; }
        public decimal Weight { get; set; }
        public string ProductionArea { get; set; } = string.Empty;

        // Reserved Materials
        public List<string> ReservedMaterials { get; set; } = new();
        public string Notes { get; set; } = string.Empty;
    }
}
