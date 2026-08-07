using AmarKajKoi.Entities;

namespace AmarKajKoi.RepositoriesInterface
{
    public interface IVoiceFileRepository
    {
        Task<Guid> CreateAsync(VoiceFile file);
        Task<VoiceFile?> GetByIdAsync(Guid voiceFileId);
        Task<int> UpdateTaskLinkAsync(Guid voiceFileId, Guid taskId);

        /// <summary>FR-39: voice files older than the retention cut-off, oldest first.</summary>
        Task<IReadOnlyList<VoiceFile>> GetOlderThanAsync(DateTime cutoffUtc, int maxRows);

        /// <summary>FR-39: removes the row after detaching it from Tasks / ExtendRequests.</summary>
        Task<int> DeleteAsync(Guid voiceFileId);
    }
}
