using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyBillingService.Models
{
    /// <summary>
    /// Phiếu nhập/xuất/kiểm kê kho thuốc.
    /// Y tá tạo phiếu (Maker), Admin duyệt phiếu (Checker).
    /// Tồn kho thực tế chỉ thay đổi khi phiếu được Admin duyệt.
    /// </summary>
    public class InventorySlip
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SlipId { get; set; }

        /// <summary>
        /// Mã phiếu tự sinh (e.g. NK-20260630-001)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string SlipCode { get; set; } = string.Empty;

        /// <summary>
        /// Loại phiếu: Import (Nhập kho), Export (Xuất hủy), Stocktake (Kiểm kê)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string SlipType { get; set; } = "Import";

        /// <summary>
        /// Trạng thái phiếu: Draft, Pending, Approved, Rejected, Voided
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Tên nhà cung cấp (nếu có)
        /// </summary>
        [MaxLength(200)]
        public string? SupplierName { get; set; }

        /// <summary>
        /// Đường dẫn ảnh chụp hóa đơn đính kèm
        /// </summary>
        [MaxLength(500)]
        public string? InvoiceImageUrl { get; set; }

        /// <summary>
        /// Ghi chú chung của phiếu
        /// </summary>
        [MaxLength(500)]
        public string? Note { get; set; }

        /// <summary>
        /// Lý do Admin từ chối (nếu bị từ chối)
        /// </summary>
        [MaxLength(500)]
        public string? RejectReason { get; set; }

        /// <summary>
        /// User ID của người tạo phiếu (Y tá)
        /// </summary>
        [Required]
        public int CreatedBy { get; set; }

        /// <summary>
        /// User ID của người duyệt/từ chối phiếu (Admin)
        /// </summary>
        public int? ApprovedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CreatedBy")]
        public User? Creator { get; set; }

        [ForeignKey("ApprovedBy")]
        public User? Approver { get; set; }

        public ICollection<InventorySlipItem> Items { get; set; } = new List<InventorySlipItem>();
    }
}
