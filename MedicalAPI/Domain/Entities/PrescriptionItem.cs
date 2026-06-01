namespace MedicalAPI.Domain.Entities;

public sealed class PrescriptionItem
{
    public int Id { get; set; }
    public string? PrescriptionItemCode { get; set; }
    public int PrescriptionId { get; set; }
    public int MedicineId { get; set; }
    public string MedicineNameSnapshot { get; set; } = string.Empty;
    public string? UnitSnapshot { get; set; }
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public decimal Quantity { get; set; }
    public string? UsageInstruction { get; set; }
    public string? Note { get; set; }
}
