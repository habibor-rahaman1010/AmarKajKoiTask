using AmarKajKoi.ServicesInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AmarKajKoi.Hubs
{
    /// <summary>
    /// Live channel for a signed-in user's notification badge.
    ///
    /// Messages sent to clients:
    ///   "unreadCount" (int) — the caller's current unread notification total.
    ///
    /// Targeting uses SignalR's user identifier, which the default provider reads
    /// from ClaimTypes.NameIdentifier — the same claim the JWT carries the user id in,
    /// so Clients.User(userId) reaches every tab that user has open.
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly INotificationService _notifications;
        public NotificationHub(INotificationService notifications) { _notifications = notifications; }

        /// <summary>
        /// The badge is correct from the moment the socket opens, so a client never
        /// has to poll: it gets the current total on connect and a fresh one on change.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = CurrentUserId();
            if (userId != Guid.Empty)
            {
                var count = await _notifications.GetUnreadCountAsync(userId);
                await Clients.Caller.SendAsync("unreadCount", count);
            }
            await base.OnConnectedAsync();
        }

        /// <summary>Lets a reconnecting client re-sync without waiting for the next change.</summary>
        public async Task<int> GetUnreadCount()
        {
            var userId = CurrentUserId();
            return userId == Guid.Empty ? 0 : await _notifications.GetUnreadCountAsync(userId);
        }

        private Guid CurrentUserId()
        {
            return Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var g) ? g : Guid.Empty;
        }
    }
}