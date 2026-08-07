using AmarKajKoi.ServicesInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmarKajKoi.Controllers
{
    [ApiController]
    [Route("api/voice")]
    [Authorize]
    public class VoiceController : AppControllerBase
    {
        private readonly IVoiceService _voice;
        public VoiceController(IVoiceService voice) { _voice = voice; }

        [HttpPost("upload")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        public async Task<IActionResult> Upload(
            [FromForm] IFormFile file,
            [FromForm] string purpose = "Target",
            [FromForm] int durationSecs = 3)
        {
            var result = await _voice.UploadAsync(CurrentUserId, purpose, file, durationSecs);
            return Ok(result);
        }

        [HttpGet("{voiceFileId:guid}")]
        public async Task<IActionResult> Download(Guid voiceFileId)
        {
            var r = await _voice.DownloadAsync(voiceFileId);
            if (r == null) return NotFound();
            // No download file name: passing one sets Content-Disposition: attachment,
            // which makes the browser save the clip instead of playing it. Range
            // processing lets a player seek without refetching the whole file.
            return File(r.Value.Content, r.Value.ContentType, enableRangeProcessing: true);
        }
    }
}
