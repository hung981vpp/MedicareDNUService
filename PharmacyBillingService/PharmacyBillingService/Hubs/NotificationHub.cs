using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PharmacyBillingService.Hubs
{
    [Authorize]
    public sealed class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Notification hub connection rejected because JWT has no NameIdentifier claim.");
                Context.Abort();
                return;
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{role}");
            }

            _logger.LogInformation("Notification hub connected for UserId {UserId}.", userId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrWhiteSpace(role))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"role:{role}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
