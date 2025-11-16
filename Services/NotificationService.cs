using Microsoft.EntityFrameworkCore;
using VehiclePricePrediction.API.Data;
using VehiclePricePrediction.API.Models;

namespace VehiclePricePrediction.API.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> CreateNotificationAsync(string userId, string type, string title, string message, string? relatedEntityType = null, string? relatedEntityId = null)
    {
        if (!Guid.TryParse(userId, out var userIdGuid))
            throw new ArgumentException("Invalid user ID");

        var notification = new Notification
        {
            Id = $"notif-{DateTime.UtcNow.Ticks}-{Guid.NewGuid().ToString("N")[..9]}",
            UserId = userIdGuid,
            Type = type,
            Title = title,
            Message = message,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return notification.Id;
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var userIdGuid))
            throw new ArgumentException("Invalid user ID");

        var notifications = await _context.Notifications
            .Where(n => n.UserId == userIdGuid)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(n => new NotificationDto
        {
            Id = n.Id,
            Type = n.Type,
            Title = n.Title,
            Message = n.Message,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
            RelatedEntityType = n.RelatedEntityType,
            RelatedEntityId = n.RelatedEntityId
        }).ToList();
    }

    public async Task<bool> MarkNotificationAsReadAsync(string notificationId, string userId)
    {
        if (!Guid.TryParse(userId, out var userIdGuid))
            throw new ArgumentException("Invalid user ID");

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userIdGuid);

        if (notification == null)
        {
            return false;
        }

        notification.IsRead = true;
        notification.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }
}

