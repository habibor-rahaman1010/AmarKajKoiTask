using AmarKajKoi.ServicesInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmarKajKoi.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : AppControllerBase
    {
        private readonly INotificationService _svc;
        public NotificationsController(INotificationService svc) { _svc = svc; }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] bool onlyUnread = false)
            => Ok(await _svc.GetForUserAsync(CurrentUserId, onlyUnread));

        /// <summary>Initial badge value; live updates arrive over the notifications hub.</summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
            => Ok(new { count = await _svc.GetUnreadCountAsync(CurrentUserId) });

        [HttpPost("{id:guid}/read")]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            await _svc.MarkReadAsync(id, CurrentUserId);
            return NoContent();
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            await _svc.MarkAllReadAsync(CurrentUserId);
            return NoContent();
        }
    }
}
