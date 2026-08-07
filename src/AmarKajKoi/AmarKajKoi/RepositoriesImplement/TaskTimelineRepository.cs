using AmarKajKoi.Database;
using AmarKajKoi.DataTransferObjects;
using AmarKajKoi.Entities;
using AmarKajKoi.RepositoriesInterface;
using Dapper;

namespace AmarKajKoi.RepositoriesImplement
{
    public class TaskTimelineRepository : ITaskTimelineRepository
    {
        private readonly IUnitOfWork _uow;
        public TaskTimelineRepository(IUnitOfWork uow) 
        { 
            _uow = uow; 
        }

        public async Task<Guid> AddAsync(TaskTimeline t)
        {
            const string sql = @"
                INSERT INTO dbo.TaskTimeline (TaskId, ActionByUserId, ActionType, FromStatusId, ToStatusId, Note)
                OUTPUT INSERTED.TimelineId
                VALUES (@TaskId, @ActionByUserId, @ActionType, @FromStatusId, @ToStatusId, @Note);";
            return await _uow.Connection.ExecuteScalarAsync<Guid>(sql, t, _uow.Transaction);
        }

        public async Task<IReadOnlyList<TimelineDto>> GetByTaskAsync(Guid taskId)
        {
            const string sql = @"
                SELECT tl.TimelineId, tl.ActionType, u.FullName AS ActionByName,
                       s1.StatusName AS FromStatus, s2.StatusName AS ToStatus,
                       tl.Note, tl.CreatedAt
                FROM dbo.TaskTimeline tl
                INNER JOIN dbo.Users u ON u.UserId = tl.ActionByUserId
                LEFT  JOIN dbo.TaskStatuses s1 ON s1.StatusId = tl.FromStatusId
                LEFT  JOIN dbo.TaskStatuses s2 ON s2.StatusId = tl.ToStatusId
                WHERE tl.TaskId = @taskId
                ORDER BY tl.CreatedAt ASC;";
            var rows = await _uow.Connection.QueryAsync<TimelineDto>(sql, new { taskId }, _uow.Transaction);
            return rows.ToList();
        }
    }
}
