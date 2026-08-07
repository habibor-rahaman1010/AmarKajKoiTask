using AmarKajKoi.Database;
using AmarKajKoi.DataTransferObjects;
using AmarKajKoi.Entities;
using AmarKajKoi.RepositoriesInterface;
using Dapper;

namespace AmarKajKoi.RepositoriesImplement
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly IUnitOfWork _uow;
        public NotificationRepository(IUnitOfWork uow) 
        { 
            _uow = uow;
        }

        public async Task<Guid> CreateAsync(Notification n)
        {
            const string sql = @"
                INSERT INTO dbo.Notifications (UserId, TaskId, Title, Body, IsRead)
                OUTPUT INSERTED.NotificationId
                VALUES (@UserId, @TaskId, @Title, @Body, 0);";
            return await _uow.Connection.ExecuteScalarAsync<Guid>(sql, n, _uow.Transaction);
        }

        public async Task CreateManyAsync(IEnumerable<Notification> ns)
        {
            const string sql = @"
                INSERT INTO dbo.Notifications (UserId, TaskId, Title, Body, IsRead)
                VALUES (@UserId, @TaskId, @Title, @Body, 0);";
            await _uow.Connection.ExecuteAsync(sql, ns, _uow.Transaction);
        }

        public async Task<IReadOnlyList<NotificationDto>> GetForUserAsync(Guid userId, bool onlyUnread)
        {
            var sql = @"
                SELECT NotificationId, TaskId, Title, Body, IsRead, CreatedAt
                FROM dbo.Notifications
                WHERE UserId=@userId " + (onlyUnread ? " AND IsRead = 0 " : string.Empty) + @"
                ORDER BY CreatedAt DESC;";
            var rows = await _uow.Connection.QueryAsync<NotificationDto>(sql, new { userId }, _uow.Transaction);
            return rows.ToList();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            const string sql = "SELECT COUNT(1) FROM dbo.Notifications WHERE UserId=@userId AND IsRead=0;";
            return await _uow.Connection.ExecuteScalarAsync<int>(sql, new { userId }, _uow.Transaction);
        }

        public async Task<IReadOnlyDictionary<Guid, int>> GetUnreadCountsAsync(IEnumerable<Guid> userIds)
        {
            var ids = userIds.Distinct().ToArray();
            if (ids.Length == 0)
            {
                return new Dictionary<Guid, int>();
            }

            const string sql = @"
                SELECT UserId, COUNT(1) AS Cnt
                FROM dbo.Notifications
                WHERE IsRead = 0 AND UserId IN @ids
                GROUP BY UserId;";

            var rows = await _uow.Connection.QueryAsync<(Guid UserId, int Cnt)>(sql, new { ids }, _uow.Transaction);

            // Users whose last unread item was just read drop out of the GROUP BY, so
            // seed every requested id at zero — they still need to be told it is zero.
            var result = ids.ToDictionary(id => id, _ => 0);
            foreach (var r in rows) 
            {
                result[r.UserId] = r.Cnt;
            }

            return result;
        }

        public async Task<int> MarkReadAsync(Guid notificationId, Guid userId)
        {
            const string sql = "UPDATE dbo.Notifications SET IsRead=1 WHERE NotificationId=@notificationId AND UserId=@userId;";
            return await _uow.Connection.ExecuteAsync(sql, new { notificationId, userId }, _uow.Transaction);
        }

        public async Task<int> MarkAllReadAsync(Guid userId)
        {
            const string sql = "UPDATE dbo.Notifications SET IsRead=1 WHERE UserId=@userId AND IsRead=0;";
            return await _uow.Connection.ExecuteAsync(sql, new { userId }, _uow.Transaction);
        }
    }
}
