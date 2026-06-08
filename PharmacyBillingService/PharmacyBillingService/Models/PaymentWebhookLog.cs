using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyBillingService.Models
{
    public class PaymentWebhookLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PaymentWebhookLogId { get; set; }

        [Required]
        [MaxLength(40)]
        public string Provider { get; set; } = "SePay";

        [MaxLength(120)]
        public string? ProviderTransactionId { get; set; }

        [MaxLength(120)]
        public string? ReferenceCode { get; set; }

        public int? InvoiceId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Received";

        [MaxLength(255)]
        public string? FailureReason { get; set; }

        public string RawPayload { get; set; } = string.Empty;

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }
    }
}
