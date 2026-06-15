using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PharmacyBillingService.Data;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Helpers;
using PharmacyBillingService.Models;
using Microsoft.IdentityModel.Tokens;
using PharmacyBillingService.Security;

namespace PharmacyBillingService.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
        Task<UserDto> RegisterAsync(RegisterDto registerDto);
        Task<CheckDuplicateResponseDto> CheckDuplicateAsync(string? username, string? email, string? phoneNumber);
        Task<UserDto?> GetProfileAsync(int userId);
        Task<UserDto?> UpdateProfileAsync(int userId, UpdateProfileDto updateDto);
        Task<List<UserDto>> GetAllUsersAsync();
        Task<List<UserDto>> GetUsersByRolesAsync(List<string> roles);
        Task<UserDto?> UpdateUserAsync(int userId, UpdateUserDto updateDto);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> LockUserAsync(int userId);
        Task<bool> UnlockUserAsync(int userId);
    }

    public class AuthService : IAuthService
    {
        private readonly PharmacyDbContext _context;
        private readonly JwtHelper _jwtHelper;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AuthService(
            PharmacyDbContext context,
            JwtHelper jwtHelper,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _jwtHelper = jwtHelper;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var identifier = !string.IsNullOrWhiteSpace(loginDto.Email)
                ? loginDto.Email.Trim()
                : !string.IsNullOrWhiteSpace(loginDto.Username)
                    ? loginDto.Username.Trim()
                    : !string.IsNullOrWhiteSpace(loginDto.EmailOrUsername)
                        ? loginDto.EmailOrUsername.Trim()
                        : loginDto.UsernameOrEmail?.Trim();

            if (string.IsNullOrWhiteSpace(identifier)) return null;

            var normalizedIdentifier = identifier.ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email.ToLower() == normalizedIdentifier ||
                u.Username.ToLower() == normalizedIdentifier);
            if (user == null) return null;

            // BR18: Chỉ tài khoản Active mới được đăng nhập
            if (user.Status != "Active")
            {
                throw new InvalidOperationException("Tài khoản đã bị khóa.");
            }

            if (!PasswordHasher.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                return null;
            }

            var token = _jwtHelper.GenerateToken(user);
            return new LoginResponseDto
            {
                Token = token,
                User = MapToUserDto(user)
            };
        }

        public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
        {
            registerDto.Role = NormalizeRole(registerDto.Role);

            // Kiểm tra trùng Email
            var emailExists = await _context.Users.AnyAsync(u => u.Email == registerDto.Email);
            if (emailExists)
            {
                throw new InvalidOperationException("[email]Email đã tồn tại trong hệ thống.");
            }

            // Kiểm tra trùng Username
            var username = string.IsNullOrWhiteSpace(registerDto.Username)
                ? BuildUsernameFromEmail(registerDto.Email)
                : registerDto.Username.Trim();

            var usernameExists = await _context.Users.AnyAsync(u => u.Username == username);
            if (usernameExists)
            {
                throw new InvalidOperationException("[username]Tên đăng nhập đã tồn tại trong hệ thống.");
            }

            // Kiểm tra trùng Số điện thoại (nếu có nhập)
            var phone = registerDto.PhoneNumber?.Trim();
            if (!string.IsNullOrWhiteSpace(phone))
            {
                var phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == phone);
                if (phoneExists)
                {
                    throw new InvalidOperationException("[phoneNumber]Số điện thoại đã tồn tại trong hệ thống.");
                }
            }

            int? patientId = null;
            if (registerDto.Role == RoleConstants.Patient)
            {
                patientId = await CreateMedicalPatientAsync(registerDto);
            }

            var user = new User
            {
                FullName = CapitalizeFullName(registerDto.FullName),
                Email = registerDto.Email,
                Username = username,
                PhoneNumber = phone,
                PatientId = patientId,
                PasswordHash = PasswordHasher.HashPassword(registerDto.Password),
                Role = registerDto.Role,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return MapToUserDto(user);
        }

        public async Task<CheckDuplicateResponseDto> CheckDuplicateAsync(string? username, string? email, string? phoneNumber)
        {
            var result = new CheckDuplicateResponseDto();

            if (!string.IsNullOrWhiteSpace(username))
            {
                result.UsernameExists = await _context.Users.AnyAsync(u => u.Username == username.Trim());
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                result.EmailExists = await _context.Users.AnyAsync(u => u.Email == email.Trim());
            }

            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                result.PhoneNumberExists = await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber.Trim());
            }

            return result;
        }

        public async Task<UserDto?> GetProfileAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;
            return MapToUserDto(user);
        }

        public async Task<UserDto?> UpdateProfileAsync(int userId, UpdateProfileDto updateDto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            var fullName = updateDto.FullName.Trim();
            var email = updateDto.Email.Trim();
            var phone = updateDto.PhoneNumber?.Trim();

            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new InvalidOperationException("[fullName]Họ tên là bắt buộc.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("[email]Email là bắt buộc.");
            }

            var normalizedEmail = email.ToLower();
            var emailExists = await _context.Users.AnyAsync(u =>
                u.UserId != userId && u.Email.ToLower() == normalizedEmail);
            if (emailExists)
            {
                throw new InvalidOperationException("[email]Email đã tồn tại trong hệ thống.");
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                var phoneExists = await _context.Users.AnyAsync(u =>
                    u.UserId != userId && u.PhoneNumber == phone);
                if (phoneExists)
                {
                    throw new InvalidOperationException("[phoneNumber]Số điện thoại đã tồn tại trong hệ thống.");
                }
            }

            user.FullName = CapitalizeFullName(fullName);
            user.Email = email;
            user.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToUserDto(user);
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
            return users.Select(MapToUserDto).ToList();
        }

        public async Task<List<UserDto>> GetUsersByRolesAsync(List<string> roles)
        {
            var users = await _context.Users
                .Where(u => roles.Contains(u.Role))
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
            return users.Select(MapToUserDto).ToList();
        }

        public async Task<UserDto?> UpdateUserAsync(int userId, UpdateUserDto updateDto)
        {
            updateDto.Role = NormalizeRole(updateDto.Role);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            var fullName = updateDto.FullName.Trim();
            var email = updateDto.Email.Trim();
            var username = string.IsNullOrWhiteSpace(updateDto.Username)
                ? BuildUsernameFromEmail(email)
                : updateDto.Username.Trim();
            var phone = updateDto.PhoneNumber?.Trim();

            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new InvalidOperationException("[fullName]Họ tên là bắt buộc.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("[email]Email là bắt buộc.");
            }

            var normalizedEmail = email.ToLower();
            var emailExists = await _context.Users.AnyAsync(u =>
                u.UserId != userId && u.Email.ToLower() == normalizedEmail);
            if (emailExists)
            {
                throw new InvalidOperationException("[email]Email đã tồn tại trong hệ thống.");
            }

            var usernameExists = await _context.Users.AnyAsync(u =>
                u.UserId != userId && u.Username == username);
            if (usernameExists)
            {
                throw new InvalidOperationException("[username]Tên đăng nhập đã tồn tại trong hệ thống.");
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                var phoneExists = await _context.Users.AnyAsync(u =>
                    u.UserId != userId && u.PhoneNumber == phone);
                if (phoneExists)
                {
                    throw new InvalidOperationException("[phoneNumber]Số điện thoại đã tồn tại trong hệ thống.");
                }
            }

            if (user.Role == RoleConstants.Admin && updateDto.Role != RoleConstants.Admin)
            {
                var adminCount = await _context.Users.CountAsync(u => u.Role == RoleConstants.Admin);
                if (adminCount <= 1)
                {
                    throw new InvalidOperationException("Không thể đổi vai trò của Admin cuối cùng.");
                }
            }

            user.FullName = CapitalizeFullName(fullName);
            user.Email = email;
            user.Username = username;
            user.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone;
            user.Role = updateDto.Role;
            user.Status = updateDto.Status;
            user.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(updateDto.Password))
            {
                user.PasswordHash = PasswordHasher.HashPassword(updateDto.Password);
            }

            await _context.SaveChangesAsync();
            return MapToUserDto(user);
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            if (user.Role == RoleConstants.Admin)
            {
                throw new InvalidOperationException("Không thể xóa tài khoản Admin.");
            }

            if (user.Role == RoleConstants.Patient && user.PatientId is int patientId)
            {
                await DeleteMedicalPatientAsync(patientId);
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LockUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            if (user.Role == RoleConstants.Admin)
            {
                throw new InvalidOperationException("Không thể khóa tài khoản Admin.");
            }

            user.Status = "Locked";
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnlockUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.Status = "Active";
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private static UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Username = user.Username,
                PhoneNumber = user.PhoneNumber,
                PatientId = user.PatientId,
                Role = user.Role,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        private static string NormalizeRole(string? role)
        {
            var normalized = role?.Trim();
            return normalized switch
            {
                RoleConstants.Admin => RoleConstants.Admin,
                RoleConstants.Doctor => RoleConstants.Doctor,
                RoleConstants.Nurse => RoleConstants.Nurse,
                RoleConstants.Pharmacist => RoleConstants.Pharmacist,
                RoleConstants.Patient => RoleConstants.Patient,
                _ => throw new InvalidOperationException("Vai tro khong hop le. Chi ho tro: Admin, Doctor, Nurse, Pharmacist, Patient.")
            };
        }

        private static string BuildUsernameFromEmail(string email)
        {
            var normalized = email.Trim();
            var atIndex = normalized.IndexOf('@');
            return atIndex > 0 ? normalized[..atIndex] : normalized;
        }

        private static string CapitalizeFullName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;
            var words = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                    words[i] = char.ToUpper(words[i][0]) + words[i][1..].ToLower();
            }
            return string.Join(' ', words);
        }

        private async Task<int> CreateMedicalPatientAsync(RegisterDto registerDto)
        {
            var medicalBaseUrl = _configuration["ServiceUrls:MedicalRecordService"] ?? "http://medical-api:8080";
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateSystemToken());

            var request = new MedicalPatientCreateRequest
            {
                FullName = CapitalizeFullName(registerDto.FullName),
                Email = registerDto.Email,
                PhoneNumber = registerDto.PhoneNumber
            };

            var response = await client.PostAsJsonAsync($"{medicalBaseUrl.TrimEnd('/')}/api/v1/medical/patients", request);
            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Không tạo được hồ sơ bệnh nhân ở N2. HTTP {(int)response.StatusCode}: {detail}");
            }

            var created = await response.Content.ReadFromJsonAsync<MedicalApiResponse<MedicalPatientDto>>();
            if (created?.Data is null || created.Data.Id <= 0)
            {
                throw new InvalidOperationException("N2 không trả về PatientId hợp lệ sau khi tạo hồ sơ bệnh nhân.");
            }

            return created.Data.Id;
        }

        private async Task DeleteMedicalPatientAsync(int patientId)
        {
            var medicalBaseUrl = _configuration["ServiceUrls:MedicalRecordService"] ?? "http://medical-api:8080";
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateSystemToken());

            var response = await client.DeleteAsync($"{medicalBaseUrl.TrimEnd('/')}/api/v1/medical/patients/{patientId}");
            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
            {
                return;
            }

            var detail = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Không thể xóa tài khoản vì hồ sơ bệnh nhân chưa được xóa. HTTP {(int)response.StatusCode}: {detail}");
        }

        private string GenerateSystemToken()
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var keyString = jwtSettings["Key"] ?? "SuperSecretKeyForPharmacyBillingServiceThatIsAtLeast32BytesLong!";
            var issuer = jwtSettings["Issuer"] ?? "PharmacyBillingService";
            var audience = jwtSettings["Audience"] ?? "PharmacyBillingService";

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(keyString);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "PharmacyAuthService"),
                    new Claim(ClaimTypes.Role, "Admin")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private sealed class MedicalPatientCreateRequest
        {
            public string FullName { get; init; } = string.Empty;
            public string? PhoneNumber { get; init; }
            public string? Email { get; init; }
        }

        private sealed class MedicalApiResponse<T>
        {
            [JsonPropertyName("data")]
            public T? Data { get; init; }
        }

        private sealed class MedicalPatientDto
        {
            [JsonPropertyName("id")]
            public int Id { get; init; }
        }

    }
}
