using AmarKajKoi.DataTransferObjects;
using AmarKajKoi.Entities;

namespace AmarKajKoi.RepositoriesInterface
{
    public interface ITaskTimelineRepository
    {
        Task<Guid> AddAsync(TaskTimeline entry);
        Task<IReadOnlyList<TimelineDto>> GetByTaskAsync(Guid taskId);
    }
}
