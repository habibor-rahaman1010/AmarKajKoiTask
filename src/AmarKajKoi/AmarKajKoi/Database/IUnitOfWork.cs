using AmarKajKoi.RepositoriesInterface;
using System.Data;

namespace AmarKajKoi.Database
{
    public interface IUnitOfWork : IDisposable
    {
        IDbConnection Connection { get; }
        IDbTransaction? Transaction { get; }

        IUserRepository Users { get; }
        ITaskRepository Tasks { get; }
        ITaskTimelineRepository Timeline { get; }
        IExtendRequestRepository ExtendRequests { get; }
        IVoiceFileRepository VoiceFiles { get; }
        INotificationRepository Notifications { get; }
        IReferenceDataRepository Reference { get; }

        void Begin();
        void Commit();
        void Rollback();
    }
}
