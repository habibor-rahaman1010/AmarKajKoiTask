using AmarKajKoi.Database;
using AmarKajKoi.DataTransferObjects;
using AmarKajKoi.Entities;
using AmarKajKoi.RepositoriesInterface;
using Dapper;

namespace AmarKajKoi.RepositoriesImplement
{
    public class ExtendRequestRepository : IExtendRequestRepository
    {
        private readonly IUnitOfWork _uow;
        public ExtendRequestRepository(IUnitOfWork uow) 
        {
            _uow = uow; 
        }

        public async Task<Guid> CreateAsync(ExtendRequest r)
        {
            const string sql = @"
                INSERT INTO dbo.ExtendRequests
                    (TaskId, RequestedByUserId, RequestType, RequestedDueDate, ReasonText, VoiceFileId, Status)
                OUTPUT INSERTED.RequestId
                VALUES
                    (@TaskId, @RequestedByUserId, @RequestType, @RequestedDueDate, @ReasonText, @VoiceFileId, @Status);";
            return await _uow.Connection.ExecuteScalarAsync<Guid>(sql, r, _uow.Transaction);
        }

        public async Task<ExtendRequest?> GetByIdAsync(Guid requestId)
        {
            const string sql = "SELECT * FROM dbo.ExtendRequests WHERE RequestId=@requestId;";
            return await _uow.Connection.QueryFirstOrDefaultAsync<ExtendRequest>(sql, new { requestId }, _uow.Transaction);
        }

        public async Task<int> UpdateDecisionAsync(Guid requestId, string status, Guid decisionByUserId, string? reason)
        {
            const string sql = @"
                UPDATE dbo.ExtendRequests
                SET Status = @status,
                    DecisionByUserId = @decisionByUserId,
                    DecisionReason = @reason,
                    DecidedAt = SYSUTCDATETIME()
                WHERE RequestId = @requestId;";
            return await _uow.Connection.ExecuteAsync(sql, new { requestId, status, decisionByUserId, reason }, _uow.Transaction);
        }

        public async Task<IReadOnlyList<ExtendRequestDto>> GetByTaskAsync(Guid taskId)
        {
            const string sql = @"
                SELECT er.RequestId, er.TaskId,
                       ru.FullName AS RequestedByName,
                       er.RequestType, er.RequestedDueDate, er.ReasonText, er.VoiceFileId,
                       er.Status, du.FullName AS DecisionByName, er.DecisionReason,
                       er.CreatedAt, er.DecidedAt
                FROM dbo.ExtendRequests er
                INNER JOIN dbo.Users ru ON ru.UserId = er.RequestedByUserId
                LEFT  JOIN dbo.Users du ON du.UserId = er.DecisionByUserId
                WHERE er.TaskId = @taskId
                ORDER BY er.CreatedAt DESC;";
            var rows = await _uow.Connection.QueryAsync<ExtendRequestDto>(sql, new { taskId }, _uow.Transaction);
            return rows.ToList();
        }

        public async Task<IReadOnlyList<ExtendRequestDto>> GetPendingForManagementAsync()
        {
            const string sql = @"
                SELECT er.RequestId, er.TaskId,
                       ru.FullName AS RequestedByName,
                       er.RequestType, er.RequestedDueDate, er.ReasonText, er.VoiceFileId,
                       er.Status, du.FullName AS DecisionByName, er.DecisionReason,
                       er.CreatedAt, er.DecidedAt
                FROM dbo.ExtendRequests er
                INNER JOIN dbo.Users ru ON ru.UserId = er.RequestedByUserId
                LEFT  JOIN dbo.Users du ON du.UserId = er.DecisionByUserId
                WHERE er.Status = 'Pending'
                ORDER BY er.CreatedAt ASC;";
            var rows = await _uow.Connection.QueryAsync<ExtendRequestDto>(sql, transaction: _uow.Transaction);
            return rows.ToList();
        }

        public async Task<bool> HasPendingAsync(Guid taskId, string requestType)
        {
            const string sql = @"
                SELECT COUNT(1) FROM dbo.ExtendRequests
                WHERE TaskId = @taskId AND RequestType = @requestType AND Status = 'Pending';";
            var count = await _uow.Connection.ExecuteScalarAsync<int>(
                sql, new { taskId, requestType }, _uow.Transaction);
            return count > 0;
        }
    }
}
