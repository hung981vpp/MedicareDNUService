using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Security;
using PharmacyBillingService.Services;

namespace PharmacyBillingService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var result = await _authService.LoginAsync(loginDto);
                return result == null
                    ? BadRequest(new { Message = "Email/username hoac mat khau khong chinh xac." })
                    : Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                registerDto.Role = RoleConstants.Patient;
                var result = await _authService.RegisterAsync(registerDto);
                return CreatedAtAction(nameof(GetProfile), new { id = result.UserId }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("users")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> CreateUser([FromBody] RegisterDto registerDto)
        {
            try
            {
                var result = await _authService.RegisterAsync(registerDto);
                return CreatedAtAction(nameof(GetProfile), new { id = result.UserId }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("users")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _authService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpPost("check-duplicate")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckDuplicate([FromBody] CheckDuplicateRequestDto request)
        {
            var result = await _authService.CheckDuplicateAsync(request.Username, request.Email, request.PhoneNumber);
            return Ok(result);
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var profile = await _authService.GetProfileAsync(userId.Value);
            return profile == null ? NotFound(new { Message = "Khong tim thay nguoi dung." }) : Ok(profile);
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateDto)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            try
            {
                var profile = await _authService.UpdateProfileAsync(userId.Value, updateDto);
                return profile == null ? NotFound(new { Message = "Khong tim thay nguoi dung." }) : Ok(profile);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("users/nurses")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> GetNurses()
        {
            var users = await _authService.GetUsersByRolesAsync(new List<string> { RoleConstants.Nurse });
            return Ok(users);
        }

        [HttpGet("users/pharmacists")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> GetPharmacists()
        {
            var users = await _authService.GetUsersByRolesAsync(new List<string> { RoleConstants.Pharmacist });
            return Ok(users);
        }

        [HttpGet("users/doctors")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> GetDoctors()
        {
            var users = await _authService.GetUsersByRolesAsync(new List<string> { RoleConstants.Doctor });
            return Ok(users);
        }

        [HttpGet("users/patients")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> GetPatients()
        {
            var users = await _authService.GetUsersByRolesAsync(new List<string> { RoleConstants.Patient });
            return Ok(users);
        }

        [HttpPut("users/{id}")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateDto)
        {
            try
            {
                var user = await _authService.UpdateUserAsync(id, updateDto);
                return user == null ? NotFound(new { Message = "Khong tim thay nguoi dung." }) : Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("users/{id}")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (GetCurrentUserId() == id)
            {
                return BadRequest(new { Message = "Khong the xoa tai khoan dang dang nhap." });
            }

            try
            {
                var success = await _authService.DeleteUserAsync(id);
                return success ? Ok(new { Message = "Xoa tai khoan thanh cong." }) : NotFound(new { Message = "Khong tim thay nguoi dung." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("users/{id}/lock")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> LockUser(int id)
        {
            try
            {
                var success = await _authService.LockUserAsync(id);
                return success ? Ok(new { Message = "Khoa tai khoan thanh cong." }) : NotFound(new { Message = "Khong tim thay nguoi dung." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("users/{id}/unlock")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> UnlockUser(int id)
        {
            var success = await _authService.UnlockUserAsync(id);
            return success ? Ok(new { Message = "Mo khoa tai khoan thanh cong." }) : NotFound(new { Message = "Khong tim thay nguoi dung." });
        }

        private int? GetCurrentUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdStr, out var userId) ? userId : null;
        }
    }
}
