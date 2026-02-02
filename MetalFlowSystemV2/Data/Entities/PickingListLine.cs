using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetalFlowSystemV2.Data.Entities
{
    public enum PickingListLineType
    {
        Coil,
        Sheet
    }

    public enum PickingListLineStatus
    {
        Open,
        Picked,
        Packed
    }

    public enum OrderUnit
    {
        PCS,
        LBS
    }

    public class PickingListLine
    {
        [Key]
        public int Id { get; set; }

        public int PickingListId { get; set; }

        [ForeignKey("PickingListId")]
        public virtual PickingList PickingList { get; set; }

        public int LineNumber { get; set; }

        public int? ItemId { get; set; }

        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }

        // We also store the raw codes/desc from the import just in case
        [Required]
        [MaxLength(50)]
        public string ItemCode { get; set; }

        [Required]
        [MaxLength(200)]
        public string Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OrderQuantity { get; set; }

        public string OrderUnit { get; set; } // Storing as string "PCS" or "LBS" as per prompt input, or Enum? Prompt input is text. I'll map to string for flexibility but the logic will determine LineType.

        [Column(TypeName = "decimal(18,2)")]
        public decimal WidthIn { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LengthIn { get; set; }

        public string LineInstructions { get; set; }

        public int ProductionAreaId { get; set; }

        [ForeignKey("ProductionAreaId")]
        public virtual ProductionArea ProductionArea { get; set; }

        public PickingListLineType LineType { get; set; }

        public PickingListLineStatus LineStatus { get; set; } = PickingListLineStatus.Open;

        public virtual ICollection<PickingListLineReservedMaterial> ReservedMaterials { get; set; } = new List<PickingListLineReservedMaterial>();
    }
}
