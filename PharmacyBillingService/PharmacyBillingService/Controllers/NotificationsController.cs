using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyBillingService.Data;
using PharmacyBillingService.DTOs;
using PharmacyBillingService.Hubs;
using PharmacyBillingService.Models;
using PharmacyBillingService.Security;

namespace PharmacyBillingService.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/notifications")]
    public sealed class NotificationsController : ControllerBase
    {
        private static readonly HashSet<string> AllowedTargetModes = new(StringComparer.OrdinalIgnoreCase)
        {
            "All",
            "Roles",
            "User"
        };

        private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            RoleConstants.Admin,
            RoleConstants.Doctor,
            RoleConstants.Nurse,
            RoleConstants.Pharmacist,
            RoleConstants.Patient
        };

        private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "System",
            "Appointment",
            "Billing",
            "Prescription",
            "MedicalRecord"
        };

        private readonly PharmacyDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationsController(PharmacyDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool? isRead = null)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId.Value);

            if (isRead is not null)
            {
                query = query.Where(n => n.IsRead == isRead.Value);
            }

            var total = await query.CountAsync();
            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var items = notifications.Select(NotificationDto.FromEntity).ToList();

            return Ok(new
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize
            });
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var count = await _context.Notifications
                .AsNoTracking()
                .CountAsync(n => n.UserId == userId.Value && !n.IsRead);

            return Ok(new { Count = count });
        }

        [HttpPost("{id:long}/read")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId.Value);

            if (notification is null) return NotFound(new { Message = "Khong tim thay thong bao." });

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Ok(NotificationDto.FromEntity(notification));
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            await _context.Notifications
                .Where(n => n.UserId == userId.Value && !n.IsRead)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));

            return Ok(new { Message = "Da danh dau tat ca thong bao la da doc." });
        }

        [HttpGet("admin/recipients")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> GetAdminRecipients([FromQuery] string? search = null)
        {
            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.Status == "Active");

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(keyword) ||
                    u.Username.ToLower().Contains(keyword) ||
                    u.Email.ToLower().Contains(keyword) ||
                    u.Role.ToLower().Contains(keyword));
            }

            var users = await query
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FullName)
                .Take(200)
                .Select(u => new NotificationRecipientDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("admin/send")]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> SendManualNotification([FromBody] CreateManualNotificationDto request)
        {
            var validationError = ValidateManualNotification(request);
            if (validationError is not null) return BadRequest(new { Message = validationError });

            var targetMode = request.TargetMode.Trim();
            var type = NormalizeNotificationType(request.Type);
            var recipientsQuery = _context.Users
                .Where(u => u.Status == "Active");

            if (targetMode.Equals("Roles", StringComparison.OrdinalIgnoreCase))
            {
                var roles = request.Roles!
                    .Select(NormalizeRole)
                    .Where(role => role is not null)
                    .Select(role => role!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                recipientsQuery = recipientsQuery.Where(u => roles.Contains(u.Role));
            }
            else if (targetMode.Equals("User", StringComparison.OrdinalIgnoreCase))
            {
                recipientsQuery = recipientsQuery.Where(u => u.UserId == request.UserId!.Value);
            }

            var recipients = await recipientsQuery
                .OrderBy(u => u.UserId)
                .Select(u => new { u.UserId, u.Role })
                .ToListAsync();

            if (!recipients.Any())
            {
                return BadRequest(new { Message = "Khong tim thay nguoi nhan dang Active phu hop." });
            }

            var notifications = recipients.Select(user => new Notification
            {
                UserId = user.UserId,
                Role = user.Role,
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                Type = type,
                ReferenceId = string.IsNullOrWhiteSpace(request.ReferenceId) ? null : request.ReferenceId.Trim(),
                NavigateUrl = string.IsNullOrWhiteSpace(request.NavigateUrl) ? "/" : request.NavigateUrl.Trim(),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            var notificationDtos = notifications.Select(NotificationDto.FromEntity).ToList();
            foreach (var notification in notificationDtos)
            {
                await _hubContext.Clients.User(notification.UserId.ToString())
                    .SendAsync("ReceiveNotification", notification);
            }

            return Ok(new ManualNotificationResponseDto
            {
                RecipientCount = notificationDtos.Count,
                Notifications = notificationDtos
            });
        }

        private int? GetCurrentUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdStr, out var userId) ? userId : null;
        }

        private static string? ValidateManualNotification(CreateManualNotificationDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Title)) return "Tieu de thong bao la bat buoc.";
            if (request.Title.Trim().Length > 200) return "Tieu de thong bao toi da 200 ky tu.";
            if (string.IsNullOrWhiteSpace(request.Content)) return "Noi dung thong bao la bat buoc.";
            if (string.IsNullOrWhiteSpace(request.TargetMode)) return "TargetMode la bat buoc.";
            if (!AllowedTargetModes.Contains(request.TargetMode.Trim())) return "TargetMode chi ho tro All, Roles hoac User.";
            if (!AllowedTypes.Contains(NormalizeNotificationType(request.Type))) return "Loai thong bao khong hop le.";

            if (request.TargetMode.Equals("Roles", StringComparison.OrdinalIgnoreCase))
            {
                if (request.Roles is null || !request.Roles.Any(role => !string.IsNullOrWhiteSpace(role)))
                {
                    return "Can chon it nhat mot vai tro khi TargetMode = Roles.";
                }

                var invalidRole = request.Roles.FirstOrDefault(role => NormalizeRole(role) is null);
                if (invalidRole is not null)
                {
                    return $"Vai tro khong hop le: {invalidRole}.";
                }
            }

            if (request.TargetMode.Equals("User", StringComparison.OrdinalIgnoreCase) && request.UserId is null)
            {
                return "UserId la bat buoc khi TargetMode = User.";
            }

            return null;
        }

        private static string NormalizeNotificationType(string? type)
        {
            var value = string.IsNullOrWhiteSpace(type) ? "System" : type.Trim();
            return AllowedTypes.FirstOrDefault(allowed => allowed.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? value;
        }

        private static string? NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return null;
            var value = role.Trim();
            return AllowedRoles.FirstOrDefault(allowed => allowed.Equals(value, StringComparison.OrdinalIgnoreCase));
        }
    }
}
