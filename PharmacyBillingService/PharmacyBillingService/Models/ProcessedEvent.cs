using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyBillingService.Models
{
    public class ProcessedEvent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProcessedEventId { get; set; }

        [Required]
        [MaxLength(160)]
        public string EventKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(80)]
        public string EventType { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? Source { get; set; }

        [MaxLength(80)]
        public string? PayloadHash { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Processing";

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }

        [MaxLength(255)]
        public string? FailureReason { get; set; }
    }
}
