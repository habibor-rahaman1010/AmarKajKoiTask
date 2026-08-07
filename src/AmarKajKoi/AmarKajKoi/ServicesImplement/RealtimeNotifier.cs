using AmarKajKoi.Database;
using AmarKajKoi.Hubs;
using AmarKajKoi.ServicesInterface;
using Microsoft.AspNetCore.SignalR;

namespace AmarKajKoi.ServicesImplement
{
    public class RealtimeNotifier : IRealtimeNotifier
    {
        private readonly IHubContext<NotificationHub> _hub;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<RealtimeNotifier> _logger;
        private readonly HashSet<Guid> _pending = new();

        public RealtimeNotifier(IHubContext<NotificationHub> hub, IUnitOfWork uow, ILogger<RealtimeNotifier> logger)
        {
            _hub = hub;
            _uow = uow;
            _logger = logger;
        }

        public void QueueUnreadRefresh(Guid userId)
        {
            if (userId != Guid.Empty)
            {
                _pending.Add(userId);
            }
        }

        public void QueueUnreadRefresh(IEnumerable<Guid> userIds)
        {
            foreach (var id in userIds)
            {
                QueueUnreadRefresh(id);
            }
        }

        public async Task FlushAsync()
        {
            if (_pending.Count == 0)
            {
                return;
            }

            var ids = _pending.ToArray();
            _pending.Clear();

            // The database work is already committed by the time this runs. A push that
            // fails — nobody connected, socket dropped mid-send — must not surface as a
            // failed request, so everything here is logged and swallowed.
            try
            {
                var counts = await _uow.Notifications.GetUnreadCountsAsync(ids);
                foreach (var id in ids)
                {
                    counts.TryGetValue(id, out var count);
                    await _hub.Clients.User(id.ToString()).SendAsync("unreadCount", count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not push unread counts to {Count} user(s)", ids.Length);
            }
        }
    }
}