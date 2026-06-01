using System.ComponentModel.DataAnnotations;

namespace AppointmentService.Dtos.Specialties;

public sealed class UpdateSpecialtyRequest
{
    [Required]
    [MaxLength(120)]
    public string SpecialtyName { get; init; } = string.Empty;
}
