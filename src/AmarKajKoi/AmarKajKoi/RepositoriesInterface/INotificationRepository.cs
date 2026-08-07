using AmarKajKoi.DataTransferObjects;
using AmarKajKoi.Entities;

namespace AmarKajKoi.RepositoriesInterface
{
    public interface INotificationRepository
    {
        Task<Guid> CreateAsync(Notification n);
        Task CreateManyAsync(IEnumerable<Notification> ns);
        Task<IReadOnlyList<NotificationDto>> GetForUserAsync(Guid userId, bool onlyUnread);
        Task<int> GetUnreadCountAsync(Guid userId);
        /// <summary>Unread counts for several users in one round trip, keyed by user id.</summary>
        Task<IReadOnlyDictionary<Guid, int>> GetUnreadCountsAsync(IEnumerable<Guid> userIds);
        Task<int> MarkReadAsync(Guid notificationId, Guid userId);
        Task<int> MarkAllReadAsync(Guid userId);
    }
}
