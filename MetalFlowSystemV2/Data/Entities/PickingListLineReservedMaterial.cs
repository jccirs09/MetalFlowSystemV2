using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MetalFlowSystemV2.Data.Entities
{
    public class PickingListLineReservedMaterial
    {
        [Key]
        public int Id { get; set; }

        public int PickingListLineId { get; set; }

        [ForeignKey("PickingListLineId")]
        public virtual PickingListLine PickingListLine { get; set; }

        [Required]
        [MaxLength(50)]
        public string TagNumber { get; set; }

        [MaxLength(50)]
        public string MillRef { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Required]
        [MaxLength(10)]
        public string Unit { get; set; } // PCS | LBS

        [MaxLength(100)]
        public string Size { get; set; } // Verbatim

        [MaxLength(50)]
        public string Location { get; set; } // Verbatim
    }
}
