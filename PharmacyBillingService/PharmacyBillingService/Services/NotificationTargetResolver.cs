using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PharmacyBillingService.Data;
using PharmacyBillingService.Security;

namespace PharmacyBillingService.Services
{
    public sealed record NotificationTarget(int UserId, string Role);

    public interface INotificationTargetResolver
    {
        Task<NotificationTarget?> ResolvePatientAsync(int patientId);
        Task<NotificationTarget?> ResolveDoctorAsync(int doctorId);
        Task<List<NotificationTarget>> ResolveRolesAsync(params string[] roles);
    }

    public sealed class NotificationTargetResolver : INotificationTargetResolver
    {
        private readonly PharmacyDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationTargetResolver> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        public NotificationTargetResolver(
            PharmacyDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<NotificationTargetResolver> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<NotificationTarget?> ResolvePatientAsync(int patientId)
        {
            var target = await _context.Users.AsNoTracking()
                .Where(u => u.Status == "Active" && u.Role == RoleConstants.Patient && u.PatientId == patientId)
                .Select(u => new NotificationTarget(u.UserId, u.Role))
                .FirstOrDefaultAsync();

            if (target is null)
            {
                _logger.LogWarning("Notification target resolver could not map PatientId {PatientId} to an active auth user.", patientId);
            }

            return target;
        }

        public async Task<NotificationTarget?> ResolveDoctorAsync(int doctorId)
        {
            var userId = await ResolveDoctorUserIdFromAppointmentServiceAsync(doctorId);
            if (userId is not null)
            {
                var target = await _context.Users.AsNoTracking()
                    .Where(u => u.Status == "Active" && u.Role == RoleConstants.Doctor && u.UserId == userId.Value)
                    .Select(u => new NotificationTarget(u.UserId, u.Role))
                    .FirstOrDefaultAsync();

                if (target is not null) return target;

                _logger.LogWarning(
                    "AppointmentService mapped DoctorId {DoctorId} to UserId {UserId}, but no active Doctor user was found.",
                    doctorId,
                    userId.Value);
            }

            var fallbackTarget = await _context.Users.AsNoTracking()
                .Where(u => u.Status == "Active" && u.Role == RoleConstants.Doctor && u.UserId == doctorId)
                .Select(u => new NotificationTarget(u.UserId, u.Role))
                .FirstOrDefaultAsync();

            if (fallbackTarget is null)
            {
                _logger.LogWarning("Notification target resolver could not map DoctorId {DoctorId} to an active auth user.", doctorId);
            }

            return fallbackTarget;
        }

        public async Task<List<NotificationTarget>> ResolveRolesAsync(params string[] roles)
        {
            var roleSet = roles.Where(role => !string.IsNullOrWhiteSpace(role)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!roleSet.Any()) return new List<NotificationTarget>();

            var targets = await _context.Users.AsNoTracking()
                .Where(u => u.Status == "Active" && roleSet.Contains(u.Role))
                .OrderBy(u => u.UserId)
                .Select(u => new NotificationTarget(u.UserId, u.Role))
                .ToListAsync();

            if (!targets.Any())
            {
                _logger.LogWarning("Notification target resolver found no active users for roles {Roles}.", string.Join(", ", roleSet));
            }

            return targets;
        }

        private async Task<int?> ResolveDoctorUserIdFromAppointmentServiceAsync(int doctorId)
        {
            var configuredBaseUrl = _configuration["ServiceUrls:AppointmentService"] ?? "http://appointment-service:8080";
            try
            {
                using var client = _httpClientFactory.CreateClient();
                using var response = await client.GetAsync($"{configuredBaseUrl.TrimEnd('/')}/api/doctors/{doctorId}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("AppointmentService did not resolve DoctorId {DoctorId}. Status {StatusCode}.", doctorId, response.StatusCode);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync();
                var payload = await JsonSerializer.DeserializeAsync<AppointmentDoctorResponse>(stream, _jsonOptions);
                return payload?.Data?.UserId > 0 ? payload.Data.UserId : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not call AppointmentService to resolve DoctorId {DoctorId}.", doctorId);
                return null;
            }
        }

        private sealed class AppointmentDoctorResponse
        {
            public AppointmentDoctorDto? Data { get; set; }
        }

        private sealed class AppointmentDoctorDto
        {
            public int? UserId { get; set; }
        }
    }
}
