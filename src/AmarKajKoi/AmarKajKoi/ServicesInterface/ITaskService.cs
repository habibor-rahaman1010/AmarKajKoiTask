using AmarKajKoi.DataTransferObjects;

namespace AmarKajKoi.ServicesInterface
{
    public interface ITaskService
    {
        Task<Guid> CreateTargetAsync(Guid userId, TargetVoiceCreateDto dto);
        Task<Guid> CreateCommitmentAsync(Guid userId, CommitmentFormCreateDto dto);
        Task EditCommitmentAsync(Guid userId, CommitmentEditDto dto);
        Task EditTargetAsync(Guid userId, TargetEditDto dto);
        Task PostTaskAsync(Guid userId, Guid taskId);
        Task DeleteDraftAsync(Guid userId, Guid taskId);

        Task ApproveCommitmentAsync(Guid userId, ApproveDto dto);
        Task RejectCommitmentAsync(Guid userId, RejectDto dto);
        Task SendBackCommitmentAsync(Guid userId, SendBackDto dto);

        Task CompleteVoiceReviewAsync(Guid userId, VoiceReviewCompleteDto dto);
        Task SendBackVoiceReviewAsync(Guid userId, SendBackDto dto);

        Task RequestExtendReviseAsync(Guid userId, ExtendRequestCreateDto dto);
        Task DecideExtendReviseAsync(Guid userId, ExtendRequestDecisionDto dto);

        Task RequestMarkPassedAsync(Guid userId, RequestMarkPassedDto dto);
        Task MarkFinalAsync(Guid userId, MarkFinalDto dto);
        Task<int> BulkMarkFinalAsync(Guid userId, BulkFinalDto dto);

        Task ChangeDueDateAsync(Guid userId, ChangeDueDateDto dto);
        Task ChangeAssigneeAsync(Guid userId, ChangeAssigneeDto dto);
        Task SetPinnedAsync(Guid userId, Guid taskId, bool pinned);
        Task AttachEmployeeVoiceAsync(Guid userId, Guid taskId, Guid voiceFileId);

        Task<TaskDetailDto?> GetDetailAsync(Guid taskId);
        Task<IReadOnlyList<TaskListItemDto>> QueryAsync(TaskFilterDto filter);
        Task<IReadOnlyList<TaskListItemDto>> GetForUserRoleAsync(Guid userId, string role, TaskFilterDto filter);
        Task<IReadOnlyList<ExtendRequestDto>> GetPendingExtendRequestsAsync();
        Task<int> RunOverdueSweepAsync();
        Task<PerformanceDto> GetSelfPerformanceAsync(Guid userId);
        Task<IReadOnlyList<PerformanceDto>> GetAllPerformanceAsync();

        Task ReopenTaskAsync(Guid userId, Guid taskId);
    }
}
