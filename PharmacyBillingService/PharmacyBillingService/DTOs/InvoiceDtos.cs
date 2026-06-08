using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PharmacyBillingService.DTOs
{
    public class CreateInvoiceDto
    {
        public int? PatientId { get; set; }

        public int? AppointmentId { get; set; }

        public int? PrescriptionId { get; set; }

        [Required(ErrorMessage = "Phi kham la bat buoc")]
        [Range(0, double.MaxValue, ErrorMessage = "Phi kham phai lon hon hoac bang 0")]
        public decimal ExaminationFee { get; set; } = 150000;
    }

    public class PaymentDto
    {
        public int PaymentId { get; set; }
        public int InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public int PaidBy { get; set; }
        public string PaidByName { get; set; } = string.Empty;
        public DateTime PaidAt { get; set; }
        public string? Note { get; set; }
    }

    public class InvoiceDto
    {
        public int InvoiceId { get; set; }
        public int PatientId { get; set; }
        public int? AppointmentId { get; set; }
        public int? PrescriptionId { get; set; }
        public decimal ExaminationFee { get; set; }
        public decimal MedicineTotal { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RefundedAmount { get; set; }
        public decimal BalanceDue { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public List<PaymentDto> Payments { get; set; } = new List<PaymentDto>();
    }

    public class PayInvoiceDto
    {
        public int? InvoiceId { get; set; }

        public decimal? Amount { get; set; }

        public string? PaymentMethod { get; set; } = "Cash";

        public string? Method { get; set; }

        [MaxLength(255, ErrorMessage = "Noi dung thanh toan toi da 255 ky tu")]
        public string? PaymentContent { get; set; }

        [MaxLength(100, ErrorMessage = "Ma ngan hang toi da 100 ky tu")]
        public string? BankCode { get; set; }

        [MaxLength(50, ErrorMessage = "So tai khoan toi da 50 ky tu")]
        public string? BankAccountNumber { get; set; }

        [MaxLength(255, ErrorMessage = "Ghi chu toi da 255 ky tu")]
        public string? Note { get; set; }
    }

    public class RefundInvoiceDto
    {
        [Range(0.01, double.MaxValue, ErrorMessage = "So tien hoan phai lon hon 0")]
        public decimal Amount { get; set; }

        [MaxLength(255, ErrorMessage = "Ly do toi da 255 ky tu")]
        public string? Reason { get; set; }
    }

    public class SePayWebhookDto
    {
        public long? Id { get; set; }
        public string? Gateway { get; set; }
        public string? TransactionDate { get; set; }
        public string? AccountNumber { get; set; }
        public string? Code { get; set; }
        public string? Content { get; set; }
        public string? TransferType { get; set; }
        public decimal TransferAmount { get; set; }
        public string? ReferenceCode { get; set; }
        public string? Description { get; set; }
    }

    public class PaymentWebhookLogDto
    {
        public int PaymentWebhookLogId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? ProviderTransactionId { get; set; }
        public string? ReferenceCode { get; set; }
        public int? InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? FailureReason { get; set; }
        public DateTime ReceivedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
