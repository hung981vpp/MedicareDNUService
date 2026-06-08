using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyBillingService.Models
{
    public class MedicineBatch
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BatchId { get; set; }

        [Required]
        public int MedicineId { get; set; }

        [Required]
        [MaxLength(80)]
        public string BatchNumber { get; set; } = string.Empty;

        public DateTime? ExpiryDate { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public int InitialQuantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ImportPrice { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("MedicineId")]
        public Medicine? Medicine { get; set; }
    }
}
