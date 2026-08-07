using AmarKajKoi.DataTransferObjects;
using AmarKajKoi.ServicesInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmarKajKoi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : AppControllerBase
    {
        private readonly IAuthService _auth;
        public AuthController(IAuthService auth) { _auth = auth; }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _auth.LoginAsync(request);
            if (result == null) return Unauthorized(new { message = "Invalid username or password." });
            return Ok(result);
        }

        [HttpPost("register")]
        [Authorize(Roles = "TopManagement,SystemAdmin")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
        {
            var user = await _auth.RegisterAsync(request);
            return Ok(user);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var me = await _auth.GetProfileAsync(CurrentUserId);
            if (me == null) return NotFound();
            return Ok(me);
        }
    }
}
