using AmarKajKoi.Entities;

namespace AmarKajKoi.RepositoriesInterface
{
    public interface IReferenceDataRepository
    {
        Task<IReadOnlyList<TaskCenter>> GetTaskCentersAsync();
        Task<IReadOnlyList<EventChannel>> GetEventChannelsAsync();
        Task<IReadOnlyList<DayEvent>> GetDayEventsAsync(Guid? eventChannelId);
        Task<IReadOnlyList<AmarKajKoi.Entities.TaskStatus>> GetStatusesAsync();
        Task<Guid> AddDayEventAsync(DayEvent ev);
    }
}
