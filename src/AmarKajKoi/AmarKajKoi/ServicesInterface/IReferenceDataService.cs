using AmarKajKoi.DataTransferObjects;
using AmarKajKoi.Entities;

namespace AmarKajKoi.ServicesInterface
{
    public interface IReferenceDataService
    {
        Task<IReadOnlyList<TaskCenter>> GetTaskCentersAsync();
        Task<IReadOnlyList<EventChannel>> GetEventChannelsAsync();
        Task<IReadOnlyList<DayEvent>> GetDayEventsAsync(Guid? eventChannelId);
        Task<IReadOnlyList<AmarKajKoi.Entities.TaskStatus>> GetStatusesAsync();
        Task<IReadOnlyList<UserRefDto>> GetAssignableEmployeesAsync();
        Task<IReadOnlyList<UserRefDto>> GetAllUsersAsync();
    }
}
