namespace VehiclePricePrediction.API.Services;

public interface INotificationService
{
    Task<string> CreateNotificationAsync(string userId, string type, string title, string message, string? relatedEntityType = null, string? relatedEntityId = null);
    Task<List<NotificationDto>> GetUserNotificationsAsync(string userId);
    Task<bool> MarkNotificationAsReadAsync(string notificationId, string userId);
}

public class NotificationDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? RelatedEntityId { get; set; }
}

