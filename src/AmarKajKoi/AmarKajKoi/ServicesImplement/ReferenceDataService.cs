using AmarKajKoi.Database;
using AmarKajKoi.DataTransferObjects;
using AmarKajKoi.Entities;
using AmarKajKoi.ServicesInterface;

namespace AmarKajKoi.ServicesImplement
{
    public class ReferenceDataService : IReferenceDataService
    {
        private readonly IUnitOfWork _uow;
        public ReferenceDataService(IUnitOfWork uow) 
        { 
            _uow = uow; 
        }

        public Task<IReadOnlyList<TaskCenter>> GetTaskCentersAsync()
        {
            return _uow.Reference.GetTaskCentersAsync();
        }

        public Task<IReadOnlyList<EventChannel>> GetEventChannelsAsync()
        {
            return _uow.Reference.GetEventChannelsAsync();
        }

        public Task<IReadOnlyList<DayEvent>> GetDayEventsAsync(Guid? eventChannelId)
        {
           return _uow.Reference.GetDayEventsAsync(eventChannelId);
        }

        public Task<IReadOnlyList<Entities.TaskStatus>> GetStatusesAsync() 
        {
            return _uow.Reference.GetStatusesAsync();
        }

        public async Task<IReadOnlyList<UserRefDto>> GetAssignableEmployeesAsync()
        { 
            return ToRefs(await _uow.Users.GetByRoleAsync("Employee"));
        }

        public async Task<IReadOnlyList<UserRefDto>> GetAllUsersAsync()
        {
            return ToRefs(await _uow.Users.GetAllAsync());
        }

        /// <summary>
        /// Projects entities onto the wire shape. The repository keeps returning full
        /// User rows because sign-in needs PasswordHash; it stops here instead.
        /// </summary>
        private static IReadOnlyList<UserRefDto> ToRefs(IReadOnlyList<User> users)
        {
            return users.Select(u => new UserRefDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Username = u.Username,
                    RoleName = u.RoleName ?? string.Empty

                }).ToList();
        }
    }
}
