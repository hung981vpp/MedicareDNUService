using System;
using System.ComponentModel.DataAnnotations;

namespace PharmacyBillingService.DTOs
{
    public class LoginDto
    {
        public string? Email { get; set; }

        public string? Username { get; set; }

        public string? EmailOrUsername { get; set; }

        public string? UsernameOrEmail { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterDto
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [MaxLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
        public string? PhoneNumber { get; set; }

        [MaxLength(50, ErrorMessage = "Username tối đa 50 ký tự")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [MaxLength(100, ErrorMessage = "Mật khẩu tối đa 100 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        [RegularExpression("^(Admin|Doctor|Nurse|Pharmacist|Patient)$", ErrorMessage = "Vai trò không hợp lệ. Phải là: Admin, Doctor, Nurse, Pharmacist, hoặc Patient")]
        public string Role { get; set; } = "Patient";
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int? PatientId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [MaxLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
        public string? PhoneNumber { get; set; }
    }

    public class UpdateUserDto
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [MaxLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "Username tối đa 50 ký tự")]
        public string? Username { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        [RegularExpression("^(Admin|Doctor|Nurse|Pharmacist|Patient)$", ErrorMessage = "Vai trò không hợp lệ. Phải là: Admin, Doctor, Nurse, Pharmacist, hoặc Patient")]
        public string Role { get; set; } = "Patient";

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [RegularExpression("^(Active|Locked)$", ErrorMessage = "Trạng thái không hợp lệ. Phải là Active hoặc Locked")]
        public string Status { get; set; } = "Active";

        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [MaxLength(100, ErrorMessage = "Mật khẩu tối đa 100 ký tự")]
        public string? Password { get; set; }
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
    }

    public class CheckDuplicateRequestDto
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class CheckDuplicateResponseDto
    {
        public bool UsernameExists { get; set; }
        public bool EmailExists { get; set; }
        public bool PhoneNumberExists { get; set; }
    }
}
