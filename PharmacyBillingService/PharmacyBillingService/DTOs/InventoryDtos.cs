using System;
using System.ComponentModel.DataAnnotations;

namespace PharmacyBillingService.DTOs
{
    public class StockImportDto
    {
        [Required(ErrorMessage = "Ma thuoc la bat buoc")]
        public int MedicineId { get; set; }

        [Required(ErrorMessage = "So luong nhap la bat buoc")]
        [Range(1, int.MaxValue, ErrorMessage = "So luong nhap phai lon hon 0")]
        public int Quantity { get; set; }

        [MaxLength(80, ErrorMessage = "Ma lo toi da 80 ky tu")]
        public string? BatchNumber { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public decimal? ImportPrice { get; set; }

        [MaxLength(255, ErrorMessage = "Ly do toi da 255 ky tu")]
        public string? Reason { get; set; }
    }

    public class StockAdjustDto
    {
        [Required(ErrorMessage = "Ma thuoc la bat buoc")]
        public int MedicineId { get; set; }

        public int? BatchId { get; set; }

        [Required(ErrorMessage = "So luong ton kho moi la bat buoc")]
        [Range(0, int.MaxValue, ErrorMessage = "So luong ton kho moi phai lon hon hoac bang 0")]
        public int NewQuantity { get; set; }

        [MaxLength(255, ErrorMessage = "Ly do toi da 255 ky tu")]
        public string? Reason { get; set; }
    }

    public class StockTransactionDto
    {
        public int TransactionId { get; set; }
        public int MedicineId { get; set; }
        public int? BatchId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string? BatchNumber { get; set; }
        public string Type { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int BeforeQuantity { get; set; }
        public int AfterQuantity { get; set; }
        public string? Reason { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MedicineBatchDto
    {
        public int BatchId { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public int Quantity { get; set; }
        public int InitialQuantity { get; set; }
        public decimal? ImportPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
