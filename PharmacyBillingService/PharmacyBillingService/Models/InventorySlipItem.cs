using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyBillingService.Models
{
    /// <summary>
    /// Chi tiết từng dòng thuốc trong một phiếu nhập/xuất kho.
    /// Mỗi dòng chứa thông tin: Thuốc nào, Số lô, Hạn dùng, Số lượng, Giá nhập.
    /// </summary>
    public class InventorySlipItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SlipItemId { get; set; }

        [Required]
        public int SlipId { get; set; }

        [Required]
        public int MedicineId { get; set; }

        /// <summary>
        /// Số lô sản xuất do Y tá nhập
        /// </summary>
        [Required]
        [MaxLength(80)]
        public string BatchNumber { get; set; } = string.Empty;

        /// <summary>
        /// Hạn sử dụng của lô thuốc
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Số lượng yêu cầu nhập/xuất
        /// </summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        /// <summary>
        /// Giá nhập vào (cho mỗi đơn vị)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ImportPrice { get; set; }

        /// <summary>
        /// Ghi chú riêng cho dòng thuốc này
        /// </summary>
        [MaxLength(255)]
        public string? Note { get; set; }

        // Navigation properties
        [ForeignKey("SlipId")]
        public InventorySlip? Slip { get; set; }

        [ForeignKey("MedicineId")]
        public Medicine? Medicine { get; set; }
    }
}
