using System.ComponentModel.DataAnnotations;

namespace PharmacyBillingService.DTOs
{
    public class GoogleLoginDto
    {
        [Required]
        public string IdToken { get; set; } = string.Empty;
    }
}
