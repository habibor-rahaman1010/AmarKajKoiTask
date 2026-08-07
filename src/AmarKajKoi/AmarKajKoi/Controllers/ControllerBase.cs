using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AmarKajKoi.Controllers
{
    public abstract class AppControllerBase : ControllerBase
    {
        protected Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var g) ? g : Guid.Empty;
        protected string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }
}
