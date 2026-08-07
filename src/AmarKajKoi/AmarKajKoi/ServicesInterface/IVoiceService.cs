using AmarKajKoi.DataTransferObjects;

namespace AmarKajKoi.ServicesInterface
{
    public interface IVoiceService
    {
        Task<VoiceUploadResponse> UploadAsync(Guid userId, string purpose, IFormFile file, int durationSecs);
        Task<(byte[] Content, string ContentType, string FileName)?> DownloadAsync(Guid voiceFileId);
    }
}
