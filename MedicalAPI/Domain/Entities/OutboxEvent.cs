using MedicalAPI.Domain.Constants;

namespace MedicalAPI.Domain.Entities;

public sealed class OutboxEvent
{
    public int Id { get; set; }
    public string? EventCode { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public int AggregateId { get; set; }
    public string Payload { get; set; } = string.Empty;
    public string Status { get; set; } = MedicalStatuses.PendingPublish;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
}
