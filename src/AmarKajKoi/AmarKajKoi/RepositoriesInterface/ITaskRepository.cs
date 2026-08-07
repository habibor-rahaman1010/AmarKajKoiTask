using AmarKajKoi.DataTransferObjects;
using AmarKajKoi.Entities;

namespace AmarKajKoi.RepositoriesInterface
{
    public interface ITaskRepository
    {
        Task<Guid> CreateAsync(TaskItem task);
        Task<TaskItem?> GetByIdAsync(Guid taskId);
        Task<int> UpdateAsync(TaskItem task);
        Task<int> UpdateStatusAsync(Guid taskId, Guid statusId);
        Task<int> UpdateAssigneeAsync(Guid taskId, Guid assigneeId);
        Task<int> UpdateDueDateAsync(Guid taskId, DateTime newDueDate);
        Task<int> IncrementRequestCountAsync(Guid taskId);
        Task<int> SetPinnedAsync(Guid taskId, bool pinned);
        Task<int> AttachVoiceAsync(Guid taskId, Guid voiceFileId);
        Task<int> SetFinalCommentAsync(Guid taskId, string? comment);
        Task<IReadOnlyList<TaskListItemDto>> QueryAsync(TaskFilterDto filter);
        Task<TaskDetailDto?> GetDetailAsync(Guid taskId);
        Task<int> MarkOverdueDueTasksAsync();
        Task<IReadOnlyList<TaskListItemDto>> GetOverdueForEscalationAsync(int overdueDays);
        Task<IReadOnlyList<TaskListItemDto>> GetPendingVoiceReviewOlderThanAsync(int hours);
    }
}