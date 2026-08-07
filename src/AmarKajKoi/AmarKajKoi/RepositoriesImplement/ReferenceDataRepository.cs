using AmarKajKoi.Database;
using AmarKajKoi.Entities;
using AmarKajKoi.RepositoriesInterface;
using Dapper;

namespace AmarKajKoi.RepositoriesImplement
{
    public class ReferenceDataRepository : IReferenceDataRepository
    {
        private readonly IUnitOfWork _uow;
        public ReferenceDataRepository(IUnitOfWork uow) { _uow = uow; }

        public async Task<IReadOnlyList<TaskCenter>> GetTaskCentersAsync()
        {
            var rows = await _uow.Connection.QueryAsync<TaskCenter>(
                "SELECT TaskCenterId, Name, IsActive FROM dbo.TaskCenters WHERE IsActive=1 ORDER BY Name;",
                transaction: _uow.Transaction);
            return rows.ToList();
        }

        public async Task<IReadOnlyList<EventChannel>> GetEventChannelsAsync()
        {
            var rows = await _uow.Connection.QueryAsync<EventChannel>(
                "SELECT EventChannelId, Name, IsActive FROM dbo.EventChannels WHERE IsActive=1 ORDER BY Name;",
                transaction: _uow.Transaction);
            return rows.ToList();
        }

        public async Task<IReadOnlyList<DayEvent>> GetDayEventsAsync(Guid? eventChannelId)
        {
            var sql = @"SELECT DayEventId, Name, EventChannelId, EventDate, IsActive
                        FROM dbo.DayEvents
                        WHERE IsActive=1 " +
                        (eventChannelId.HasValue ? " AND EventChannelId = @eventChannelId " : string.Empty) +
                        " ORDER BY Name;";
            var rows = await _uow.Connection.QueryAsync<DayEvent>(sql, new { eventChannelId }, _uow.Transaction);
            return rows.ToList();
        }

        public async Task<IReadOnlyList<AmarKajKoi.Entities.TaskStatus>> GetStatusesAsync()
        {
            var rows = await _uow.Connection.QueryAsync<AmarKajKoi.Entities.TaskStatus>(
                "SELECT StatusId, StatusCode, StatusName FROM dbo.TaskStatuses ORDER BY StatusCode;",
                transaction: _uow.Transaction);
            return rows.ToList();
        }

        public async Task<Guid> AddDayEventAsync(DayEvent ev)
        {
            const string sql = @"
                INSERT INTO dbo.DayEvents (Name, EventChannelId, EventDate, IsActive)
                OUTPUT INSERTED.DayEventId
                VALUES (@Name, @EventChannelId, @EventDate, 1);";
            return await _uow.Connection.ExecuteScalarAsync<Guid>(sql, ev, _uow.Transaction);
        }
    }
}