using AmarKajKoi.Database;
using AmarKajKoi.Entities;
using AmarKajKoi.RepositoriesInterface;
using Dapper;

namespace AmarKajKoi.RepositoriesImplement
{
    public class VoiceFileRepository : IVoiceFileRepository
    {
        private readonly IUnitOfWork _uow;
        public VoiceFileRepository(IUnitOfWork uow) 
        { 
            _uow = uow; 
        }

        public async Task<Guid> CreateAsync(VoiceFile voiceFile)
        {
            const string sql = @"
                INSERT INTO dbo.VoiceFiles
                    (TaskId, UploadedByUserId, FileName, StoragePath, DurationSecs, Purpose, TranscribedText)
                OUTPUT INSERTED.VoiceFileId
                VALUES
                    (@TaskId, @UploadedByUserId, @FileName, @StoragePath, @DurationSecs, @Purpose, @TranscribedText);";
            return await _uow.Connection.ExecuteScalarAsync<Guid>(sql, voiceFile, _uow.Transaction);
        }

        public async Task<VoiceFile?> GetByIdAsync(Guid voiceFileId)
        {
            const string sql = "SELECT * FROM dbo.VoiceFiles WHERE VoiceFileId=@voiceFileId;";
            return await _uow.Connection.QueryFirstOrDefaultAsync<VoiceFile>(sql, new { voiceFileId }, _uow.Transaction);
        }

        public async Task<int> UpdateTaskLinkAsync(Guid voiceFileId, Guid taskId)
        {
            const string sql = "UPDATE dbo.VoiceFiles SET TaskId=@taskId WHERE VoiceFileId=@voiceFileId;";
            return await _uow.Connection.ExecuteAsync(sql, new { voiceFileId, taskId }, _uow.Transaction);
        }

        public async Task<IReadOnlyList<VoiceFile>> GetOlderThanAsync(DateTime cutoffUtc, int maxRows)
        {
            const string sql = @"
                SELECT TOP (@maxRows) *
                FROM dbo.VoiceFiles
                WHERE CreatedAt < @cutoffUtc
                ORDER BY CreatedAt ASC;";
            var rows = await _uow.Connection.QueryAsync<VoiceFile>(sql, new { cutoffUtc, maxRows }, _uow.Transaction);
            return rows.ToList();
        }

        public async Task<int> DeleteAsync(Guid voiceFileId)
        {
            // Tasks.VoiceFileId has no FK but ExtendRequests.VoiceFileId does, so both
            // references are cleared before the row goes away.
            const string sql = @"
                UPDATE dbo.Tasks SET VoiceFileId = NULL WHERE VoiceFileId = @voiceFileId;
                UPDATE dbo.ExtendRequests SET VoiceFileId = NULL WHERE VoiceFileId = @voiceFileId;
                DELETE FROM dbo.VoiceFiles WHERE VoiceFileId = @voiceFileId;";
            return await _uow.Connection.ExecuteAsync(sql, new { voiceFileId }, _uow.Transaction);
        }
    }
}
