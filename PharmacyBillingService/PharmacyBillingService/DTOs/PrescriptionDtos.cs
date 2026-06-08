using System;
using System.Collections.Generic;

namespace PharmacyBillingService.DTOs
{
    public class PrescriptionItemDto
    {
        public int PrescriptionItemId { get; set; }
        public int PrescriptionId { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? Dosage { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public int? CurrentStock { get; set; }
        public bool? IsAvailable { get; set; }
        public int? ShortageQuantity { get; set; }
        public string? StockStatus { get; set; }
    }

    public class PrescriptionDto
    {
        public int PrescriptionId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public int? AppointmentId { get; set; }
        public int? MedicalRecordId { get; set; }
        public string Status { get; set; } = string.Empty; // Pending, ReadyToDispense, Dispensed, OutOfStock, PartiallyAvailable
        public DateTime CreatedAt { get; set; }
        public string? StockStatus { get; set; }
        public string? InvoiceStatus { get; set; }
        public bool CanApprove { get; set; }
        public bool CanDispense { get; set; }
        public List<PrescriptionItemDto> PrescriptionItems { get; set; } = new List<PrescriptionItemDto>();
    }

    public class PrescriptionStockItemDto
    {
        public int PrescriptionItemId { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public int RequiredQuantity { get; set; }
        public int CurrentStock { get; set; }
        public int ShortageQuantity { get; set; }
        public bool IsAvailable { get; set; }
        public string StockStatus { get; set; } = string.Empty;
    }

    public class PrescriptionStockCheckDto
    {
        public int PrescriptionId { get; set; }
        public int PatientId { get; set; }
        public string PrescriptionStatus { get; set; } = string.Empty;
        public string? InvoiceStatus { get; set; }
        public bool AllAvailable { get; set; }
        public bool AnyAvailable { get; set; }
        public bool CanApprove { get; set; }
        public bool CanDispense { get; set; }
        public List<PrescriptionStockItemDto> Items { get; set; } = new();
    }
}
