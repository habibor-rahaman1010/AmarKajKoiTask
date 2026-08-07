using AmarKajKoi.Database;
using AmarKajKoi.DataTransferObjects;
using AmarKajKoi.ServicesInterface;

namespace AmarKajKoi.ServicesImplement
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _uow;
        private readonly IRealtimeNotifier _realtime;

        public NotificationService(IUnitOfWork uow, IRealtimeNotifier realtime)
        {
            _uow = uow;
            _realtime = realtime;
        }

        public Task<IReadOnlyList<NotificationDto>> GetForUserAsync(Guid userId, bool onlyUnread)
        {
            return _uow.Notifications.GetForUserAsync(userId, onlyUnread);
        }

        public Task<int> GetUnreadCountAsync(Guid userId)
        {
            return _uow.Notifications.GetUnreadCountAsync(userId);
        }

        public async Task MarkReadAsync(Guid notificationId, Guid userId)
        {
            _uow.Begin();
            try 
            { 
                await _uow.Notifications.MarkReadAsync(notificationId, userId); 
                _uow.Commit(); 
            }
            catch 
            { 
                _uow.Rollback(); 
                throw; 
            }

            // Reading is a change to the badge too: other tabs must count down as well.
            _realtime.QueueUnreadRefresh(userId);
        }

        public async Task MarkAllReadAsync(Guid userId)
        {
            _uow.Begin();
            try { await _uow.Notifications.MarkAllReadAsync(userId); _uow.Commit(); }
            catch { _uow.Rollback(); throw; }

            _realtime.QueueUnreadRefresh(userId);
        }
    }
}
