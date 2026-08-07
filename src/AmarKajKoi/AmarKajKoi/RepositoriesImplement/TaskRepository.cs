using AmarKajKoi.Database;
using AmarKajKoi.DataTransferObjects;
using AmarKajKoi.Entities;
using AmarKajKoi.RepositoriesInterface;
using Dapper;
using System.Text;

namespace AmarKajKoi.RepositoriesImplement
{
    public class TaskRepository : ITaskRepository
    {
        private readonly IUnitOfWork _uow;

        public TaskRepository(IUnitOfWork uow) 
        { 
            _uow = uow; 
        }

        public async Task<Guid> CreateAsync(TaskItem t)
        {
            const string sql = @"
                INSERT INTO dbo.Tasks
                    (TaskName, TaskType, TaskCenterId, EventChannelId, DayEventId,
                     CreatedByUserId, AssignedToUserId, DueDate, StatusId,
                     RequestCount, IsPinned, VoiceFileId, VoiceAutoText, Description)
                OUTPUT INSERTED.TaskId
                VALUES
                    (@TaskName, @TaskType, @TaskCenterId, @EventChannelId, @DayEventId,
                     @CreatedByUserId, @AssignedToUserId, @DueDate, @StatusId,
                     @RequestCount, @IsPinned, @VoiceFileId, @VoiceAutoText, @Description);";
            return await _uow.Connection.ExecuteScalarAsync<Guid>(sql, t, _uow.Transaction);
        }

        public async Task<TaskItem?> GetByIdAsync(Guid taskId)
        {
            const string sql = @"SELECT * FROM dbo.Tasks WHERE TaskId = @taskId;";
            return await _uow.Connection.QueryFirstOrDefaultAsync<TaskItem>(sql, new { taskId }, _uow.Transaction);
        }

        public async Task<int> UpdateAsync(TaskItem t)
        {
            const string sql = @"
                UPDATE dbo.Tasks SET
                    TaskName = @TaskName,
                    TaskCenterId = @TaskCenterId,
                    EventChannelId = @EventChannelId,
                    DayEventId = @DayEventId,
                    AssignedToUserId = @AssignedToUserId,
                    DueDate = @DueDate,
                    Description = @Description,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE TaskId = @TaskId;";
            return await _uow.Connection.ExecuteAsync(sql, t, _uow.Transaction);
        }

        public async Task<int> UpdateStatusAsync(Guid taskId, Guid statusId)
        {
            const string sql = "UPDATE dbo.Tasks SET StatusId=@statusId, UpdatedAt=SYSUTCDATETIME() WHERE TaskId=@taskId;";
            return await _uow.Connection.ExecuteAsync(sql, new { taskId, statusId }, _uow.Transaction);
        }

        public async Task<int> UpdateAssigneeAsync(Guid taskId, Guid assigneeId)
        {
            const string sql = "UPDATE dbo.Tasks SET AssignedToUserId=@assigneeId, UpdatedAt=SYSUTCDATETIME() WHERE TaskId=@taskId;";
            return await _uow.Connection.ExecuteAsync(sql, new { taskId, assigneeId }, _uow.Transaction);
        }

        public async Task<int> UpdateDueDateAsync(Guid taskId, DateTime newDueDate)
        {
            const string sql = "UPDATE dbo.Tasks SET DueDate=@newDueDate, UpdatedAt=SYSUTCDATETIME() WHERE TaskId=@taskId;";
            return await _uow.Connection.ExecuteAsync(sql, new { taskId, newDueDate }, _uow.Transaction);
        }

        public async Task<int> IncrementRequestCountAsync(Guid taskId)
        {
            const string sql = "UPDATE dbo.Tasks SET RequestCount = RequestCount + 1, UpdatedAt=SYSUTCDATETIME() WHERE TaskId=@taskId;";
            return await _uow.Connection.ExecuteAsync(sql, new { taskId }, _uow.Transaction);
        }

        public async Task<int> SetPinnedAsync(Guid taskId, bool pinned)
        {
            const string sql = "UPDATE dbo.Tasks SET IsPinned=@pinned, UpdatedAt=SYSUTCDATETIME() WHERE TaskId=@taskId;";
            return await _uow.Connection.ExecuteAsync(sql, new { taskId, pinned }, _uow.Transaction);
        }

        public async Task<int> AttachVoiceAsync(Guid taskId, Guid voiceFileId)
        {
            const string sql = "UPDATE dbo.Tasks SET VoiceFileId=@voiceFileId, UpdatedAt=SYSUTCDATETIME() WHERE TaskId=@taskId;";
            return await _uow.Connection.ExecuteAsync(sql, new { taskId, voiceFileId }, _uow.Transaction);
        }

        public async Task<int> SetFinalCommentAsync(Guid taskId, string? comment)
        {
            const string sql = "UPDATE dbo.Tasks SET FinalComment=@comment, UpdatedAt=SYSUTCDATETIME() WHERE TaskId=@taskId;";
            return await _uow.Connection.ExecuteAsync(sql, new { taskId, comment }, _uow.Transaction);
        }

        private const string TaskListSelect = @"
            SELECT t.TaskId, t.TaskName, t.TaskType,
                   tc.Name AS TaskCenter, ec.Name AS EventChannel, de.Name AS DayEvent,
                   ua.FullName AS AssignedToName, t.AssignedToUserId,
                   uc.FullName AS CreatedByName, t.CreatedByUserId,
                   t.DueDate, s.StatusCode, s.StatusName, t.StatusId,
                   t.RequestCount, t.IsPinned, t.CreatedAt
            FROM dbo.Tasks t
            INNER JOIN dbo.TaskStatuses s ON s.StatusId = t.StatusId
            LEFT  JOIN dbo.TaskCenters tc ON tc.TaskCenterId = t.TaskCenterId
            LEFT  JOIN dbo.EventChannels ec ON ec.EventChannelId = t.EventChannelId
            LEFT  JOIN dbo.DayEvents de ON de.DayEventId = t.DayEventId
            LEFT  JOIN dbo.Users ua ON ua.UserId = t.AssignedToUserId
            INNER JOIN dbo.Users uc ON uc.UserId = t.CreatedByUserId";

        public async Task<IReadOnlyList<TaskListItemDto>> QueryAsync(TaskFilterDto f)
        {
            var sql = new StringBuilder(TaskListSelect);
            sql.Append(" WHERE 1 = 1 ");
            var p = new DynamicParameters();
            if (f.StatusId.HasValue)         
            { 
                sql.Append(" AND t.StatusId = @StatusId ");                 
                p.Add("StatusId", f.StatusId.Value); 
            }

            if (f.AssignedToUserId.HasValue) 
            { 
                sql.Append(" AND t.AssignedToUserId = @AssignedToUserId "); 
                p.Add("AssignedToUserId", f.AssignedToUserId.Value); 
            }

            if (f.CreatedByUserId.HasValue)  
            { 
                sql.Append(" AND t.CreatedByUserId = @CreatedByUserId ");  
                p.Add("CreatedByUserId", f.CreatedByUserId.Value); 
            }

            if (f.EventChannelId.HasValue)   
            { 
                sql.Append(" AND t.EventChannelId = @EventChannelId ");    
                p.Add("EventChannelId", f.EventChannelId.Value); 
            }

            if (f.FromDate.HasValue)         
            { 
                sql.Append(" AND t.CreatedAt >= @FromDate ");             
                p.Add("FromDate", f.FromDate.Value); 
            }

            if (f.ToDate.HasValue)           
            { 
                sql.Append(" AND t.CreatedAt <  @ToDate ");                 
                p.Add("ToDate", f.ToDate.Value.AddDays(1)); 
            }

            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                sql.Append(" AND t.TaskName LIKE @Search ");
                p.Add("Search", "%" + f.Search + "%");
            }

            sql.Append(" ORDER BY t.IsPinned DESC, t.CreatedAt DESC ");

            var rows = await _uow.Connection.QueryAsync<TaskListItemDto>(sql.ToString(), p, _uow.Transaction);
            return rows.ToList();
        }

        public async Task<TaskDetailDto?> GetDetailAsync(Guid taskId)
        {
            const string detailSql = @"
                SELECT t.TaskId, t.TaskName, t.TaskType,
                       tc.Name AS TaskCenter, ec.Name AS EventChannel, de.Name AS DayEvent,
                       ua.FullName AS AssignedToName, t.AssignedToUserId,
                       uc.FullName AS CreatedByName, t.CreatedByUserId,
                       t.DueDate, s.StatusCode, s.StatusName, t.StatusId,
                       t.RequestCount, t.IsPinned, t.CreatedAt,
                       t.Description, t.VoiceAutoText, t.VoiceFileId, t.FinalComment
                FROM dbo.Tasks t
                INNER JOIN dbo.TaskStatuses s ON s.StatusId = t.StatusId
                LEFT  JOIN dbo.TaskCenters tc ON tc.TaskCenterId = t.TaskCenterId
                LEFT  JOIN dbo.EventChannels ec ON ec.EventChannelId = t.EventChannelId
                LEFT  JOIN dbo.DayEvents de ON de.DayEventId = t.DayEventId
                LEFT  JOIN dbo.Users ua ON ua.UserId = t.AssignedToUserId
                INNER JOIN dbo.Users uc ON uc.UserId = t.CreatedByUserId
                WHERE t.TaskId = @taskId;";
            return await _uow.Connection.QueryFirstOrDefaultAsync<TaskDetailDto>(detailSql, new { taskId }, _uow.Transaction);
        }

        public async Task<int> MarkOverdueDueTasksAsync()
        {
            const string sql = @"
                UPDATE dbo.Tasks
                SET StatusId = @overdue, UpdatedAt = SYSUTCDATETIME()
                WHERE DueDate IS NOT NULL
                  AND DueDate < SYSUTCDATETIME()
                  AND StatusId IN (@openStatus, @requestStatus);";
            return await _uow.Connection.ExecuteAsync(sql, new
            {
                overdue       = TaskStatusIds.Overdue,
                openStatus    = TaskStatusIds.Open,
                requestStatus = TaskStatusIds.RequestToExtendRevise
            }, _uow.Transaction);
        }

        public async Task<IReadOnlyList<TaskListItemDto>> GetOverdueForEscalationAsync(int overdueDays)
        {
            const string sql = @"
                SELECT t.TaskId, t.TaskName, t.TaskType,
                       tc.Name AS TaskCenter, ec.Name AS EventChannel, de.Name AS DayEvent,
                       ua.FullName AS AssignedToName, t.AssignedToUserId,
                       uc.FullName AS CreatedByName, t.CreatedByUserId,
                       t.DueDate, s.StatusCode, s.StatusName, t.StatusId,
                       t.RequestCount, t.IsPinned, t.CreatedAt
                FROM dbo.Tasks t
                INNER JOIN dbo.TaskStatuses s ON s.StatusId = t.StatusId
                LEFT  JOIN dbo.TaskCenters tc ON tc.TaskCenterId = t.TaskCenterId
                LEFT  JOIN dbo.EventChannels ec ON ec.EventChannelId = t.EventChannelId
                LEFT  JOIN dbo.DayEvents de ON de.DayEventId = t.DayEventId
                LEFT  JOIN dbo.Users ua ON ua.UserId = t.AssignedToUserId
                INNER JOIN dbo.Users uc ON uc.UserId = t.CreatedByUserId
                WHERE t.StatusId = @overdue
                  AND t.DueDate < DATEADD(DAY, -@days, SYSUTCDATETIME());";
            var rows = await _uow.Connection.QueryAsync<TaskListItemDto>(sql, new { overdue = TaskStatusIds.Overdue, days = overdueDays }, _uow.Transaction);
            return rows.ToList();
        }

        public async Task<IReadOnlyList<TaskListItemDto>> GetPendingVoiceReviewOlderThanAsync(int hours)
        {
            const string sql = @"
                SELECT t.TaskId, t.TaskName, t.TaskType,
                       tc.Name AS TaskCenter, ec.Name AS EventChannel, de.Name AS DayEvent,
                       ua.FullName AS AssignedToName, t.AssignedToUserId,
                       uc.FullName AS CreatedByName, t.CreatedByUserId,
                       t.DueDate, s.StatusCode, s.StatusName, t.StatusId,
                       t.RequestCount, t.IsPinned, t.CreatedAt
                FROM dbo.Tasks t
                INNER JOIN dbo.TaskStatuses s ON s.StatusId = t.StatusId
                LEFT  JOIN dbo.TaskCenters tc ON tc.TaskCenterId = t.TaskCenterId
                LEFT  JOIN dbo.EventChannels ec ON ec.EventChannelId = t.EventChannelId
                LEFT  JOIN dbo.DayEvents de ON de.DayEventId = t.DayEventId
                LEFT  JOIN dbo.Users ua ON ua.UserId = t.AssignedToUserId
                INNER JOIN dbo.Users uc ON uc.UserId = t.CreatedByUserId
                WHERE t.StatusId = @pendingVoice
                  AND t.CreatedAt < DATEADD(HOUR, -@hours, SYSUTCDATETIME());";
            var rows = await _uow.Connection.QueryAsync<TaskListItemDto>(sql,
                new { pendingVoice = TaskStatusIds.PendingVoiceReview, hours }, _uow.Transaction);
            return rows.ToList();
        }
    }
}
