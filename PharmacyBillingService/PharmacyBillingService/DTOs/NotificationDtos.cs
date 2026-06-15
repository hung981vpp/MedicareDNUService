using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PharmacyBillingService.Models;

namespace PharmacyBillingService.DTOs
{
    public sealed class CreateManualNotificationDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Type { get; set; } = "System";

        [MaxLength(255)]
        public string? NavigateUrl { get; set; }

        [MaxLength(100)]
        public string? ReferenceId { get; set; }

        [Required]
        public string TargetMode { get; set; } = "All";

        public List<string>? Roles { get; set; }

        public int? UserId { get; set; }
    }

    public sealed class NotificationDto
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? ReferenceId { get; set; }
        public string NavigateUrl { get; set; } = "/";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public static NotificationDto FromEntity(Notification notification)
            => new()
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Role = notification.Role,
                Title = notification.Title,
                Content = notification.Content,
                Type = notification.Type,
                ReferenceId = notification.ReferenceId,
                NavigateUrl = notification.NavigateUrl,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };
    }

    public sealed class ManualNotificationResponseDto
    {
        public int RecipientCount { get; set; }
        public List<NotificationDto> Notifications { get; set; } = new();
    }

    public sealed class NotificationRecipientDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
