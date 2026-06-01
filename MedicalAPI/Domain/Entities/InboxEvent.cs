using MedicalAPI.Domain.Constants;

namespace MedicalAPI.Domain.Entities;

public sealed class InboxEvent
{
    public int Id { get; set; }
    public string EventCode { get; set; } = string.Empty;
    public string SourceService { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = MedicalStatuses.Processed;
    public string? ErrorMessage { get; set; }
}
