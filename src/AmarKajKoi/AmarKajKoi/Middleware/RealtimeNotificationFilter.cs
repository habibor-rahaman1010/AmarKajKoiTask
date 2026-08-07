using AmarKajKoi.ServicesInterface;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AmarKajKoi.Middleware
{
    /// <summary>
    /// Flushes queued notification badge updates once a controller action has finished.
    ///
    /// This is the point where every service in the request has committed its
    /// transaction, so the unread counts read here are the ones the user will also
    /// see if they reload. Actions that threw are skipped — their writes rolled back,
    /// so there is nothing to announce.
    /// </summary>
    public sealed class RealtimeNotificationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executed = await next();
            if (executed.Exception != null && !executed.ExceptionHandled)
            {
                return;
            }

            var notifier = context.HttpContext.RequestServices.GetRequiredService<IRealtimeNotifier>();
            await notifier.FlushAsync();
        }
    }
}
