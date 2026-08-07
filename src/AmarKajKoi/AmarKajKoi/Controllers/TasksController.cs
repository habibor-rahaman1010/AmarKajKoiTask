using AmarKajKoi.DataTransferObjects;
using AmarKajKoi.ServicesInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmarKajKoi.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    [Authorize]
    public class TasksController : AppControllerBase
    {
        private readonly ITaskService _tasks;
        public TasksController(ITaskService tasks) { _tasks = tasks; }

        // ---------- QUERY ----------
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] TaskFilterDto filter)
            => Ok(await _tasks.GetForUserRoleAsync(CurrentUserId, CurrentRole, filter ?? new TaskFilterDto()));

        [HttpGet("all")]
        [Authorize(Roles = "TopManagement,SystemAdmin")]
        public async Task<IActionResult> All([FromQuery] TaskFilterDto filter) => Ok(await _tasks.QueryAsync(filter ?? new TaskFilterDto()));

        /// <summary>Tasks the current user CREATED (regardless of assignment). Screen 11: My Commitments.</summary>
        [HttpGet("mine")]
        public async Task<IActionResult> Mine([FromQuery] TaskFilterDto filter)
        {
            var f = filter ?? new TaskFilterDto();
            f.CreatedByUserId = CurrentUserId;
            f.AssignedToUserId = null;
            return Ok(await _tasks.QueryAsync(f));
        }

        [HttpGet("{taskId:guid}")]
        public async Task<IActionResult> Get(Guid taskId)
        {
            var d = await _tasks.GetDetailAsync(taskId);
            if (d == null) return NotFound();
            return Ok(d);
        }

        // ---------- CREATE ----------
        [HttpPost("target")]
        [Authorize(Roles = "TopManagement")]
        public async Task<IActionResult> CreateTarget([FromBody] TargetVoiceCreateDto dto)
            => Ok(new { taskId = await _tasks.CreateTargetAsync(CurrentUserId, dto) });

        [HttpPost("commitment")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> CreateCommitment([FromBody] CommitmentFormCreateDto dto)
            => Ok(new { taskId = await _tasks.CreateCommitmentAsync(CurrentUserId, dto) });

        [HttpPut("commitment")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> EditCommitment([FromBody] CommitmentEditDto dto)
        {
            await _tasks.EditCommitmentAsync(CurrentUserId, dto);
            return NoContent();
        }

        [HttpPut("target")]
        [Authorize(Roles = "TopManagement")]
        public async Task<IActionResult> EditTarget([FromBody] TargetEditDto dto)
        {
            await _tasks.EditTargetAsync(CurrentUserId, dto);
            return NoContent();
        }

        [HttpPost("{taskId:guid}/post")]
        public async Task<IActionResult> Post(Guid taskId)
        {
            await _tasks.PostTaskAsync(CurrentUserId, taskId);
            return NoContent();
        }

        [HttpDelete("{taskId:guid}")]
        public async Task<IActionResult> DeleteDraft(Guid taskId)
        {
            await _tasks.DeleteDraftAsync(CurrentUserId, taskId);
            return NoContent();
        }

        // ---------- APPROVAL ----------
        [HttpPost("approve")]
        [Authorize(Roles = "TopManagement")]
        public async Task<IActionResult> Approve([FromBody] ApproveDto dto)
        {
            await _tasks.ApproveCommitmentAsync(CurrentUserId, dto);
            return NoContent();
        }

        [HttpPost("reject")]
        [Authorize(Roles = "TopManagement")]
        public async Task<IActionResult> Reject([FromBody] RejectDto dto)
        {
            await _tasks.RejectCommitmentAsync(CurrentUserId, dto);
            return NoContent();
        }

        [HttpPost("send-back")]
        [Authorize(Roles = "TopManagement")]
        public async Task<IActionResult> SendBack([FromBody] SendBackDto dto)
        {
            await _tasks.SendBackCommitmentAsync(CurrentUserId, dto);
            return NoContent();
        }

        // ---------- VOICE REVIEW ----------
        [HttpPost("voice-review/complete")]
        [Authorize(Roles = "VoiceReviewer")]
        public async Task<IActionResult> VoiceReviewComplete([FromBody] VoiceReviewCompleteDto dto)
        {
            await _tasks.CompleteVoiceReviewAsync(CurrentUserId, dto);
            return NoContent();
        }

        [HttpPost("voice-review/send-back")]
        [Authorize(Roles = "VoiceReviewer")]
        public async Task<IActionResult> VoiceReviewSendBack([FromBody] SendBackDto dto)
        {
            await _tasks.SendBackVoiceReviewAsync(CurrentUserId, dto);
            return NoContent();
        }

        // ---------- EXTEND / REVISE ----------
        [HttpPost("extend-request")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> RequestExtend([FromBody] ExtendRequestCreateDto dto)
        {
            await _tasks.RequestExtendReviseAsync(CurrentUserId, dto);
            return NoContent();
        }

        [HttpGet("extend-request/pending")]
        [Authorize(Roles = "TopManagement")]
        public async Task<IActionResult> PendingExtends() => Ok(await _tasks.GetPendingExtendRequestsAsync());

        [HttpPost("extend-request/decide")]
        [Authorize(Roles = "TopManagement")]
        public async Task<IActionResult> DecideExtend([FromBody] ExtendRequestDecisionDto dto)
        {
            await _tasks.DecideExtendReviseAsync(CurrentUserId, dto);
            return NoContent();
        }

        // ---------- FINAL DECISIONS ----------
        [HttpPost("request-mark-passed")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> RequestMarkPassed([FromBody] RequestMarkPassedDto dto)
        {
            await _tasks.RequestMarkPassedAsync(CurrentUserId, dto);
            return NoContent();
        }

        [HttpPost("final")]
        [Authorize(Roles = "TopManagement")]
        public async Task<IActionResult> MarkFinal([FromBody] MarkFinalDto dto)
        {
            await _tasks.MarkFinalAsync(CurrentUserId, dto);
            return NoContent();
        }

        [HttpPost("bulk-final")]
        [Authorize(Roles = "TopManagement")]
        public async Task<IActionResult> BulkFinal([FromBody] BulkFinalDto dto)
            => Ok(new { affected = await _tasks.BulkMarkFinalAsync(CurrentUserId, dto) });

        // ---------- MISC MGMT ----------
        [HttpPost("due-date")]
        [Authorize(Roles = "TopManagement")]
        public async Task<IActionResult> ChangeDueDate([FromBody] ChangeDueDateDto dto)
        {
            await _tasks.ChangeDueDateAsync(CurrentUserId, dto);
            return NoContent();
        }

        [HttpPost("assignee")]
        [Authorize(Roles = "TopManagement")]
        public async Task<IActionResult> ChangeAssignee([FromBody] ChangeAssigneeDto dto)
        {
            await _tasks.ChangeAssigneeAsync(CurrentUserId, dto);
            return NoContent();
        }

        [HttpPost("{taskId:guid}/pin")]
        [Authorize(Roles = "TopManagement")]
        public async Task<IActionResult> Pin(Guid taskId, [FromQuery] bool pinned = true)
        {
            await _tasks.SetPinnedAsync(CurrentUserId, taskId, pinned);
            return NoContent();
        }

        [HttpPost("{taskId:guid}/voice-commitment/{voiceFileId:guid}")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> AttachVoice(Guid taskId, Guid voiceFileId)
        {
            await _tasks.AttachEmployeeVoiceAsync(CurrentUserId, taskId, voiceFileId);
            return NoContent();
        }

        [HttpPost("run-overdue-sweep")]
        [Authorize(Roles = "TopManagement,SystemAdmin")]
        public async Task<IActionResult> OverdueSweep() => Ok(new { affected = await _tasks.RunOverdueSweepAsync() });

        [HttpPost("{taskId:guid}/reopen")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<IActionResult> Reopen(Guid taskId)
        {
            await _tasks.ReopenTaskAsync(CurrentUserId, taskId);
            return NoContent();
        }

        // ---------- PERFORMANCE ----------
        [HttpGet("performance/self")]
        public async Task<IActionResult> PerfSelf() => Ok(await _tasks.GetSelfPerformanceAsync(CurrentUserId));

        [HttpGet("performance/all")]
        [Authorize(Roles = "TopManagement")]
        public async Task<IActionResult> PerfAll() => Ok(await _tasks.GetAllPerformanceAsync());
    }
}
