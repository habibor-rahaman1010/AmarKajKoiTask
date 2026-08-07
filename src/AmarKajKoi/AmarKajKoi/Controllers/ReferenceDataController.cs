using AmarKajKoi.ServicesInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmarKajKoi.Controllers
{
    [ApiController]
    [Route("api/reference")]
    [Authorize]
    public class ReferenceDataController : AppControllerBase
    {
        private readonly IReferenceDataService _reference;
        public ReferenceDataController(IReferenceDataService reference) { _reference = reference; }

        [HttpGet("task-centers")]
        public async Task<IActionResult> TaskCenters() => Ok(await _reference.GetTaskCentersAsync());

        [HttpGet("event-channels")]
        public async Task<IActionResult> EventChannels() => Ok(await _reference.GetEventChannelsAsync());

        [HttpGet("day-events")]
        public async Task<IActionResult> DayEvents([FromQuery] Guid? eventChannelId)
            => Ok(await _reference.GetDayEventsAsync(eventChannelId));

        [HttpGet("statuses")]
        public async Task<IActionResult> Statuses() => Ok(await _reference.GetStatusesAsync());

        [HttpGet("employees")]
        public async Task<IActionResult> Employees() => Ok(await _reference.GetAssignableEmployeesAsync());

        [HttpGet("users")]
        [Authorize(Roles = "TopManagement,SystemAdmin")]
        public async Task<IActionResult> Users() => Ok(await _reference.GetAllUsersAsync());
    }
}
