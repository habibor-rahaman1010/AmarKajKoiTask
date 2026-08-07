using AmarKajKoi.DataTransferObjects;

namespace AmarKajKoi.ServicesInterface
{
    public interface INotificationService
    {
        Task<IReadOnlyList<NotificationDto>> GetForUserAsync(Guid userId, bool onlyUnread);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task MarkReadAsync(Guid notificationId, Guid userId);
        Task MarkAllReadAsync(Guid userId);
    }
}
