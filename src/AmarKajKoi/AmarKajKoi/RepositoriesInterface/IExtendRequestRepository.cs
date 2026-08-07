using AmarKajKoi.DataTransferObjects;
using AmarKajKoi.Entities;

namespace AmarKajKoi.RepositoriesInterface
{
    public interface IExtendRequestRepository
    {
        Task<Guid> CreateAsync(ExtendRequest req);
        Task<ExtendRequest?> GetByIdAsync(Guid requestId);
        Task<int> UpdateDecisionAsync(Guid requestId, string status, Guid decisionByUserId, string? reason);
        Task<IReadOnlyList<ExtendRequestDto>> GetByTaskAsync(Guid taskId);
        Task<IReadOnlyList<ExtendRequestDto>> GetPendingForManagementAsync();

        /// <summary>True when the task already has an undecided request of this type.</summary>
        Task<bool> HasPendingAsync(Guid taskId, string requestType);
    }
}
