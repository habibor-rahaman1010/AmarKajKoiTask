using AmarKajKoi.RepositoriesImplement;
using AmarKajKoi.RepositoriesInterface;
using System.Data;

namespace AmarKajKoi.Database
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly IDbConnection _connection;
        private IDbTransaction? _transaction;
        private bool _disposed;

        private IUserRepository? _users;
        private ITaskRepository? _tasks;
        private ITaskTimelineRepository? _timeline;
        private IExtendRequestRepository? _extendRequests;
        private IVoiceFileRepository? _voiceFiles;
        private INotificationRepository? _notifications;
        private IReferenceDataRepository? _reference;

        public UnitOfWork(IDbConnectionFactory factory)
        {
            _connection = factory.CreateConnection();
            _connection.Open();
        }

        public IDbConnection Connection => _connection;
        public IDbTransaction? Transaction => _transaction;

        public IUserRepository Users => _users ??= new UserRepository(this);
        public ITaskRepository Tasks  => _tasks ??= new TaskRepository(this);
        public ITaskTimelineRepository Timeline => _timeline ??= new TaskTimelineRepository(this);
        public IExtendRequestRepository ExtendRequests => _extendRequests ??= new ExtendRequestRepository(this);
        public IVoiceFileRepository VoiceFiles  => _voiceFiles ??= new VoiceFileRepository(this);
        public INotificationRepository Notifications => _notifications ??= new NotificationRepository(this);
        public IReferenceDataRepository Reference => _reference    ??= new ReferenceDataRepository(this);

        public void Begin()
        {
            _transaction ??= _connection.BeginTransaction();
        }

        public void Commit()
        {
            try
            {
                _transaction?.Commit();
            }
            catch
            {
                _transaction?.Rollback();
                throw;
            }
            finally
            {
                _transaction?.Dispose();
                _transaction = null;
            }
        }

        public void Rollback()
        {
            try
            {
                _transaction?.Rollback();
            }
            finally
            {
                _transaction?.Dispose();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _transaction?.Dispose();
            _connection.Dispose();
            _disposed = true;
        }
    }
}
