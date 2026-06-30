using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PharmacyBillingService.DTOs
{
    // ─── Request DTOs ───

    /// <summary>
    /// DTO để Y tá tạo phiếu yêu cầu nhập kho
    /// </summary>
    public class CreateInventorySlipDto
    {
        [MaxLength(200)]
        public string? SupplierName { get; set; }

        [MaxLength(500)]
        public string? InvoiceImageUrl { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Phiếu nhập kho phải có ít nhất 1 dòng thuốc")]
        [MinLength(1, ErrorMessage = "Phiếu nhập kho phải có ít nhất 1 dòng thuốc")]
        public List<CreateInventorySlipItemDto> Items { get; set; } = new();
    }

    public class CreateInventorySlipItemDto
    {
        [Required(ErrorMessage = "Mã thuốc là bắt buộc")]
        public int MedicineId { get; set; }

        [Required(ErrorMessage = "Số lô sản xuất là bắt buộc")]
        [MaxLength(80)]
        public string BatchNumber { get; set; } = string.Empty;

        public DateTime? ExpiryDate { get; set; }

        [Required(ErrorMessage = "Số lượng nhập là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng nhập phải lớn hơn 0")]
        public int Quantity { get; set; }

        public decimal? ImportPrice { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }
    }

    /// <summary>
    /// DTO để Admin duyệt phiếu
    /// </summary>
    public class ApproveInventorySlipDto
    {
        [MaxLength(500)]
        public string? Note { get; set; }
    }

    /// <summary>
    /// DTO để Admin từ chối phiếu
    /// </summary>
    public class RejectInventorySlipDto
    {
        [Required(ErrorMessage = "Lý do từ chối là bắt buộc")]
        [MaxLength(500)]
        public string RejectReason { get; set; } = string.Empty;
    }

    // ─── Response DTOs ───

    public class InventorySlipDto
    {
        public int SlipId { get; set; }
        public string SlipCode { get; set; } = string.Empty;
        public string SlipType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? SupplierName { get; set; }
        public string? InvoiceImageUrl { get; set; }
        public string? Note { get; set; }
        public string? RejectReason { get; set; }
        public int CreatedBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public int? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int TotalItems { get; set; }
        public int TotalQuantity { get; set; }
        public List<InventorySlipItemDto> Items { get; set; } = new();
    }

    public class InventorySlipItemDto
    {
        public int SlipItemId { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string? MedicineUnit { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public int Quantity { get; set; }
        public decimal? ImportPrice { get; set; }
        public string? Note { get; set; }
    }
}
