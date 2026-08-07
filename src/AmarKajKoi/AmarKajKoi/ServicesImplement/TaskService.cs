using AmarKajKoi.Database;
using AmarKajKoi.DataTransferObjects;
using AmarKajKoi.Entities;
using AmarKajKoi.ServicesInterface;

namespace AmarKajKoi.ServicesImplement
{
    public class TaskService : ITaskService
    {
        private readonly IUnitOfWork _uow;
        private readonly IRealtimeNotifier _realtime;

        public TaskService(IUnitOfWork uow, IRealtimeNotifier realtime)
        {
            _uow = uow;
            _realtime = realtime;
        }

        // ---- Helpers ----
        private async Task LogAsync(Guid taskId, Guid userId, string action, Guid? from = null, Guid? to = null, string? note = null)
        {
            await _uow.Timeline.AddAsync(new TaskTimeline
            {
                TaskId = taskId,
                ActionByUserId = userId,
                ActionType = action,
                FromStatusId = from,
                ToStatusId = to,
                Note = note
            });
        }

        private async Task NotifyAsync(Guid userId, Guid taskId, string title, string? body)
        {
            await _uow.Notifications.CreateAsync(new Notification
            {
                UserId = userId,
                TaskId = taskId,
                Title = title,
                Body = body
            });
            _realtime.QueueUnreadRefresh(userId);
        }

        // ---- CREATE ----
        public async Task<Guid> CreateTargetAsync(Guid userId, TargetVoiceCreateDto dto)
        {
            var task = new TaskItem
            {
                TaskName = string.IsNullOrWhiteSpace(dto.TaskName) ? "(Voice Target)" : dto.TaskName,
                TaskType = "Target",
                CreatedByUserId = userId,
                Description = dto.Description,
                VoiceFileId = dto.VoiceFileId,
                StatusId = dto.PostImmediately ? TaskStatusIds.PendingVoiceReview : TaskStatusIds.Draft
            };

            _uow.Begin();
            try
            {
                task.TaskId = await _uow.Tasks.CreateAsync(task);
                if (dto.VoiceFileId.HasValue)
                    await _uow.VoiceFiles.UpdateTaskLinkAsync(dto.VoiceFileId.Value, task.TaskId);
                await LogAsync(task.TaskId, userId, "Created", null, task.StatusId, "Target created.");
                if (dto.PostImmediately)
                {
                    await LogAsync(task.TaskId, userId, "PostedForVoiceReview", TaskStatusIds.Draft, TaskStatusIds.PendingVoiceReview);
                    await NotifyReviewersAsync(task.TaskId, "New voice target pending review");
                }
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }

            return task.TaskId;
        }

        public async Task<Guid> CreateCommitmentAsync(Guid userId, CommitmentFormCreateDto dto)
        {
            if (dto.DueDate.HasValue && dto.DueDate.Value < DateTime.UtcNow.Date)
                throw new InvalidOperationException("Due date cannot be in the past.");

            var task = new TaskItem
            {
                TaskName = dto.TaskName,
                TaskType = "Commitment",
                TaskCenterId = dto.TaskCenterId,
                EventChannelId = dto.EventChannelId,
                DayEventId = dto.DayEventId,
                DueDate = dto.DueDate,
                CreatedByUserId = userId,
                AssignedToUserId = userId,
                Description = dto.Description,
                VoiceFileId = dto.VoiceFileId,
                StatusId = dto.PostImmediately ? TaskStatusIds.PendingManagementApproval : TaskStatusIds.Draft
            };

            _uow.Begin();
            try
            {
                task.TaskId = await _uow.Tasks.CreateAsync(task);
                if (dto.VoiceFileId.HasValue)
                    await _uow.VoiceFiles.UpdateTaskLinkAsync(dto.VoiceFileId.Value, task.TaskId);
                await LogAsync(task.TaskId, userId, "Created", null, task.StatusId, "Commitment created.");
                if (dto.PostImmediately)
                {
                    await LogAsync(task.TaskId, userId, "PostedForApproval", TaskStatusIds.Draft, TaskStatusIds.PendingManagementApproval);
                    await NotifyManagementAsync(task.TaskId, "New commitment awaiting your approval");
                }
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }

            return task.TaskId;
        }

        public async Task EditCommitmentAsync(Guid userId, CommitmentEditDto dto)
        {
            var task = await _uow.Tasks.GetByIdAsync(dto.TaskId) ?? throw new InvalidOperationException("Task not found.");
            if (task.CreatedByUserId != userId)
                throw new UnauthorizedAccessException("Only creator can edit commitment.");
            if (task.StatusId != TaskStatusIds.Draft && task.StatusId != TaskStatusIds.PendingManagementApproval)
                throw new InvalidOperationException("Only Draft or Pending Approval commitments can be edited.");
            if (dto.DueDate.HasValue && dto.DueDate.Value < DateTime.UtcNow.Date)
                throw new InvalidOperationException("Due date cannot be in the past.");

            task.TaskName = dto.TaskName;
            task.Description = dto.Description;
            task.TaskCenterId = dto.TaskCenterId;
            task.EventChannelId = dto.EventChannelId;
            task.DayEventId = dto.DayEventId;
            task.DueDate = dto.DueDate;

            _uow.Begin();
            try
            {
                await _uow.Tasks.UpdateAsync(task);
                await LogAsync(task.TaskId, userId, "Edited", null, null, "Commitment updated.");
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }
        }

        public async Task EditTargetAsync(Guid userId, TargetEditDto dto)
        {
            var task = await _uow.Tasks.GetByIdAsync(dto.TaskId) ?? throw new InvalidOperationException("Task not found.");
            if (task.CreatedByUserId != userId)
                throw new UnauthorizedAccessException("Only creator can edit target.");
            if (task.StatusId != TaskStatusIds.Draft)
                throw new InvalidOperationException("Only Draft targets can be edited.");
            if (task.TaskType != "Target")
                throw new InvalidOperationException("Not a Target task.");

            task.TaskName = string.IsNullOrWhiteSpace(dto.TaskName) ? task.TaskName : dto.TaskName;
            task.Description = dto.Description;
            if (dto.VoiceFileId.HasValue) task.VoiceFileId = dto.VoiceFileId;

            _uow.Begin();
            try
            {
                await _uow.Tasks.UpdateAsync(task);
                if (dto.VoiceFileId.HasValue)
                    await _uow.VoiceFiles.UpdateTaskLinkAsync(dto.VoiceFileId.Value, task.TaskId);
                await LogAsync(task.TaskId, userId, "Edited", null, null, "Target updated.");
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }
        }

        public async Task PostTaskAsync(Guid userId, Guid taskId)
        {
            var task = await _uow.Tasks.GetByIdAsync(taskId) ?? throw new InvalidOperationException("Task not found.");
            if (task.StatusId != TaskStatusIds.Draft)
                throw new InvalidOperationException("Only Draft can be posted.");
            if (task.CreatedByUserId != userId)
                throw new UnauthorizedAccessException("Only creator can post.");

            var nextStatus = task.TaskType == "Target"
                ? TaskStatusIds.PendingVoiceReview
                : TaskStatusIds.PendingManagementApproval;

            _uow.Begin();
            try
            {
                await _uow.Tasks.UpdateStatusAsync(taskId, nextStatus);
                await LogAsync(taskId, userId, "Posted", task.StatusId, nextStatus);
                if (nextStatus == TaskStatusIds.PendingVoiceReview)
                    await NotifyReviewersAsync(taskId, "New voice target pending review");
                else
                    await NotifyManagementAsync(taskId, "New commitment awaiting your approval");
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }
        }

        public async Task DeleteDraftAsync(Guid userId, Guid taskId)
        {
            var task = await _uow.Tasks.GetByIdAsync(taskId) ?? throw new InvalidOperationException("Task not found.");
            if (task.StatusId != TaskStatusIds.Draft || task.CreatedByUserId != userId)
                throw new InvalidOperationException("Only Draft created by user can be deleted.");
            _uow.Begin();
            try
            {
                await _uow.Tasks.UpdateStatusAsync(taskId, TaskStatusIds.Cancelled);
                await LogAsync(taskId, userId, "DraftDeleted", task.StatusId, TaskStatusIds.Cancelled);
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }
        }

        // ---- APPROVAL FLOW ----
        public async Task ApproveCommitmentAsync(Guid userId, ApproveDto dto)
        {
            var task = await _uow.Tasks.GetByIdAsync(dto.TaskId) ?? throw new InvalidOperationException("Task not found.");
            if (task.StatusId != TaskStatusIds.PendingManagementApproval)
                throw new InvalidOperationException("Task is not pending management approval.");

            _uow.Begin();
            try
            {
                if (dto.AssignedToUserId.HasValue)
                    await _uow.Tasks.UpdateAssigneeAsync(dto.TaskId, dto.AssignedToUserId.Value);
                await _uow.Tasks.UpdateStatusAsync(dto.TaskId, TaskStatusIds.Open);
                await LogAsync(dto.TaskId, userId, "Approved", task.StatusId, TaskStatusIds.Open);

                var assignee = dto.AssignedToUserId ?? task.AssignedToUserId;
                if (assignee.HasValue)
                    await NotifyAsync(assignee.Value, dto.TaskId, "Your task is now Open", task.TaskName);
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }
        }

        public async Task RejectCommitmentAsync(Guid userId, RejectDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new InvalidOperationException("Reason is required to reject.");
            var task = await _uow.Tasks.GetByIdAsync(dto.TaskId) ?? throw new InvalidOperationException("Task not found.");
            if (task.StatusId != TaskStatusIds.PendingManagementApproval)
                throw new InvalidOperationException("Task is not pending approval.");

            _uow.Begin();
            try
            {
                await _uow.Tasks.UpdateStatusAsync(dto.TaskId, TaskStatusIds.Cancelled);
                await LogAsync(dto.TaskId, userId, "Rejected", task.StatusId, TaskStatusIds.Cancelled, dto.Reason);
                await NotifyAsync(task.CreatedByUserId, dto.TaskId, "Your commitment was rejected", dto.Reason);
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }
        }

        public async Task SendBackCommitmentAsync(Guid userId, SendBackDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new InvalidOperationException("Reason is required to send back.");
            var task = await _uow.Tasks.GetByIdAsync(dto.TaskId) ?? throw new InvalidOperationException("Task not found.");
            if (task.StatusId != TaskStatusIds.PendingManagementApproval)
                throw new InvalidOperationException("Task is not pending approval.");

            _uow.Begin();
            try
            {
                await _uow.Tasks.UpdateStatusAsync(dto.TaskId, TaskStatusIds.Draft);
                await LogAsync(dto.TaskId, userId, "SentBack", task.StatusId, TaskStatusIds.Draft, dto.Reason);
                await NotifyAsync(task.CreatedByUserId, dto.TaskId, "Your commitment was sent back", dto.Reason);
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }
        }

        // ---- VOICE REVIEW ----
        public async Task CompleteVoiceReviewAsync(Guid userId, VoiceReviewCompleteDto dto)
        {
            var task = await _uow.Tasks.GetByIdAsync(dto.TaskId) ?? throw new InvalidOperationException("Task not found.");
            if (task.StatusId != TaskStatusIds.PendingVoiceReview)
                throw new InvalidOperationException("Task is not pending voice review.");
            if (dto.DueDate < DateTime.UtcNow.Date)
                throw new InvalidOperationException("Due date cannot be in the past.");

            task.TaskName = dto.TaskName;
            task.TaskCenterId = dto.TaskCenterId;
            task.EventChannelId = dto.EventChannelId;
            task.DayEventId = dto.DayEventId;
            task.DueDate = dto.DueDate;
            task.AssignedToUserId = dto.AssignedToUserId;

            _uow.Begin();
            try
            {
                await _uow.Tasks.UpdateAsync(task);
                await _uow.Tasks.UpdateStatusAsync(dto.TaskId, TaskStatusIds.Open);
                await LogAsync(dto.TaskId, userId, "VoiceReviewCompleted", task.StatusId, TaskStatusIds.Open);
                await NotifyAsync(dto.AssignedToUserId, dto.TaskId, "New task assigned to you", task.TaskName);
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }
        }

        public async Task SendBackVoiceReviewAsync(Guid userId, SendBackDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new InvalidOperationException("Reason is required.");
            var task = await _uow.Tasks.GetByIdAsync(dto.TaskId) ?? throw new InvalidOperationException("Task not found.");
            if (task.StatusId != TaskStatusIds.PendingVoiceReview)
                throw new InvalidOperationException("Task is not pending voice review.");

            _uow.Begin();
            try
            {
                await _uow.Tasks.UpdateStatusAsync(dto.TaskId, TaskStatusIds.Draft);
                await LogAsync(dto.TaskId, userId, "VoiceReviewSentBack", task.StatusId, TaskStatusIds.Draft, dto.Reason);
                await NotifyAsync(task.CreatedByUserId, dto.TaskId, "Voice target sent back for correction", dto.Reason);
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }
        }

        // ---- EXTEND / REVISE ----
        public async Task RequestExtendReviseAsync(Guid userId, ExtendRequestCreateDto dto)
        {
            var task = await _uow.Tasks.GetByIdAsync(dto.TaskId) ?? throw new InvalidOperationException("Task not found.");
            if (task.AssignedToUserId != userId)
                throw new UnauthorizedAccessException("Only assignee can request extend/revise.");
            if (task.StatusId != TaskStatusIds.Open && task.StatusId != TaskStatusIds.Overdue)
                throw new InvalidOperationException("Only Open or Overdue tasks can request extend/revise.");
            if (task.RequestCount >= 3)
                throw new InvalidOperationException("Maximum 3 requests reached.");
            // FR-20: every extend/revise request requires a voice reason.
            if (!dto.VoiceFileId.HasValue)
                throw new InvalidOperationException("Voice reason is required for Extend/Revise requests.");
            // FR-31: a requested due date can never be in the past.
            if (dto.RequestedDueDate.HasValue && dto.RequestedDueDate.Value < DateTime.UtcNow.Date)
                throw new InvalidOperationException("Requested due date cannot be in the past.");
            // An Extend is meaningless without the date it extends to.
            if (dto.RequestType == "Extend" && !dto.RequestedDueDate.HasValue)
                throw new InvalidOperationException("A new due date is required for an Extend request.");
            if (await _uow.ExtendRequests.HasPendingAsync(dto.TaskId, dto.RequestType))
                throw new InvalidOperationException("A request of this type is already awaiting a decision.");

            _uow.Begin();
            try
            {
                await _uow.ExtendRequests.CreateAsync(new ExtendRequest
                {
                    TaskId = dto.TaskId,
                    RequestedByUserId = userId,
                    RequestType = dto.RequestType,
                    RequestedDueDate = dto.RequestedDueDate,
                    ReasonText = dto.ReasonText,
                    VoiceFileId = dto.VoiceFileId,
                    Status = "Pending"
                });
                await _uow.Tasks.IncrementRequestCountAsync(dto.TaskId);
                await _uow.Tasks.UpdateStatusAsync(dto.TaskId, TaskStatusIds.RequestToExtendRevise);
                await LogAsync(dto.TaskId, userId, "ExtendRequested", task.StatusId, TaskStatusIds.RequestToExtendRevise,
                               $"Type: {dto.RequestType}");
                await NotifyManagementAsync(dto.TaskId, "Extend/Revise request awaiting your decision");
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }
        }

        public async Task DecideExtendReviseAsync(Guid userId, ExtendRequestDecisionDto dto)
        {
            var req = await _uow.ExtendRequests.GetByIdAsync(dto.RequestId)
                       ?? throw new InvalidOperationException("Request not found.");
            if (req.Status != "Pending")
                throw new InvalidOperationException("Request already decided.");
            if (!dto.Approve && string.IsNullOrWhiteSpace(dto.Reason))
                throw new InvalidOperationException("Reason is required to reject.");

            var task = await _uow.Tasks.GetByIdAsync(req.TaskId) ?? throw new InvalidOperationException("Task not found.");

            _uow.Begin();
            try
            {
                await _uow.ExtendRequests.UpdateDecisionAsync(dto.RequestId,
                    dto.Approve ? "Approved" : "Rejected", userId, dto.Reason);

                if (req.RequestType == "MarkPassed")
                {
                    if (dto.Approve)
                    {
                        await _uow.Tasks.UpdateStatusAsync(task.TaskId, TaskStatusIds.Passed);
                        await LogAsync(task.TaskId, userId, "MarkedAsPassed", task.StatusId, TaskStatusIds.Passed, "Approved employee request");
                    }
                    else
                    {
                        await LogAsync(task.TaskId, userId, "MarkPassedRejected", null, null, dto.Reason);
                    }
                }
                else
                {
                    if (dto.Approve)
                    {
                        if (req.RequestedDueDate.HasValue)
                            await _uow.Tasks.UpdateDueDateAsync(task.TaskId, req.RequestedDueDate.Value);
                        await _uow.Tasks.UpdateStatusAsync(task.TaskId, TaskStatusIds.Open);
                        await LogAsync(task.TaskId, userId, "ExtendApproved", TaskStatusIds.RequestToExtendRevise, TaskStatusIds.Open);
                    }
                    else
                    {
                        await _uow.Tasks.UpdateStatusAsync(task.TaskId, TaskStatusIds.Open);
                        await LogAsync(task.TaskId, userId, "ExtendRejected", TaskStatusIds.RequestToExtendRevise, TaskStatusIds.Open, dto.Reason);
                    }
                }

                if (task.AssignedToUserId.HasValue)
                {
                    var title = req.RequestType == "MarkPassed"
                        ? (dto.Approve ? "Task marked as Passed" : "Mark-as-Passed request rejected")
                        : (dto.Approve ? "Extend/Revise approved" : "Extend/Revise rejected");
                    await NotifyAsync(task.AssignedToUserId.Value, task.TaskId, title, dto.Reason);
                }
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }
        }

        // ---- FINAL DECISIONS ----
        public async Task RequestMarkPassedAsync(Guid userId, RequestMarkPassedDto dto)
        {
            var task = await _uow.Tasks.GetByIdAsync(dto.TaskId) ?? throw new InvalidOperationException("Task not found.");
            if (task.AssignedToUserId != userId)
                throw new UnauthorizedAccessException("Only assignee can request Mark as Passed.");
            if (task.StatusId != TaskStatusIds.Open && task.StatusId != TaskStatusIds.Overdue)
                throw new InvalidOperationException("Only Open or Overdue tasks can request Mark as Passed.");
            if (await _uow.ExtendRequests.HasPendingAsync(dto.TaskId, "MarkPassed"))
                throw new InvalidOperationException("A Mark as Passed request is already awaiting a decision.");

            _uow.Begin();
            try
            {
                // Reuse ExtendRequests table with RequestType='MarkPassed' so it lands in the
                // same "pending decisions" queue for Management to Approve/Reject (FR-26).
                await _uow.ExtendRequests.CreateAsync(new ExtendRequest
                {
                    TaskId = dto.TaskId,
                    RequestedByUserId = userId,
                    RequestType = "MarkPassed",
                    ReasonText = dto.Note,
                    Status = "Pending"
                });
                await LogAsync(dto.TaskId, userId, "RequestMarkPassed", null, null, dto.Note);
                await NotifyManagementAsync(dto.TaskId, "Employee requested Mark as Passed");
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }
        }

        public async Task MarkFinalAsync(Guid userId, MarkFinalDto dto)
        {
            var task = await _uow.Tasks.GetByIdAsync(dto.TaskId) ?? throw new InvalidOperationException("Task not found.");
            var newStatus = dto.Decision switch
            {
                "Passed"    => TaskStatusIds.Passed,
                "Failed"    => TaskStatusIds.Failed,
                "Cancelled" => TaskStatusIds.Cancelled,
                _            => throw new InvalidOperationException("Invalid decision.")
            };
            if (task.StatusId == TaskStatusIds.Passed || task.StatusId == TaskStatusIds.Failed || task.StatusId == TaskStatusIds.Cancelled)
                throw new InvalidOperationException("Task is already in a final state.");

            _uow.Begin();
            try
            {
                await _uow.Tasks.UpdateStatusAsync(dto.TaskId, newStatus);
                if (!string.IsNullOrWhiteSpace(dto.Comment))
                    await _uow.Tasks.SetFinalCommentAsync(dto.TaskId, dto.Comment);
                await LogAsync(dto.TaskId, userId, $"MarkedAs{dto.Decision}", task.StatusId, newStatus, dto.Comment);
                if (task.AssignedToUserId.HasValue)
                    await NotifyAsync(task.AssignedToUserId.Value, dto.TaskId, $"Task marked as {dto.Decision}", dto.Comment);
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }
        }

        public async Task<int> BulkMarkFinalAsync(Guid userId, BulkFinalDto dto)
        {
            var count = 0;
            foreach (var id in dto.TaskIds.Distinct())
            {
                try
                {
                    await MarkFinalAsync(userId, new MarkFinalDto { TaskId = id, Decision = dto.Decision, Comment = dto.Comment });
                    count++;
                }
                catch { /* skip failing rows in bulk */ }
            }
            return count;
        }

        public async Task ChangeDueDateAsync(Guid userId, ChangeDueDateDto dto)
        {
            if (dto.NewDueDate < DateTime.UtcNow.Date)
                throw new InvalidOperationException("Due date cannot be in the past.");
            var task = await _uow.Tasks.GetByIdAsync(dto.TaskId) ?? throw new InvalidOperationException("Task not found.");

            _uow.Begin();
            try
            {
                var prev = task.DueDate?.ToString("yyyy-MM-dd") ?? "(none)";
                await _uow.Tasks.UpdateDueDateAsync(dto.TaskId, dto.NewDueDate);
                await LogAsync(dto.TaskId, userId, "DueDateChanged", null, null,
                    $"From {prev} to {dto.NewDueDate:yyyy-MM-dd}. {dto.Note}");
                if (task.AssignedToUserId.HasValue)
                    await NotifyAsync(task.AssignedToUserId.Value, dto.TaskId, "Due date updated", dto.NewDueDate.ToString("yyyy-MM-dd"));
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }
        }

        public async Task ChangeAssigneeAsync(Guid userId, ChangeAssigneeDto dto)
        {
            var task = await _uow.Tasks.GetByIdAsync(dto.TaskId) ?? throw new InvalidOperationException("Task not found.");
            _uow.Begin();
            try
            {
                await _uow.Tasks.UpdateAssigneeAsync(dto.TaskId, dto.NewAssigneeUserId);
                await LogAsync(dto.TaskId, userId, "AssigneeChanged", null, null,
                    $"Previous: {task.AssignedToUserId?.ToString() ?? "(none)"}, New: {dto.NewAssigneeUserId}");
                await NotifyAsync(dto.NewAssigneeUserId, dto.TaskId, "You have a new task assigned", task.TaskName);
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }
        }

        public async Task SetPinnedAsync(Guid userId, Guid taskId, bool pinned)
        {
            _uow.Begin();
            try
            {
                await _uow.Tasks.SetPinnedAsync(taskId, pinned);
                await LogAsync(taskId, userId, pinned ? "Pinned" : "Unpinned");
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }
        }

        public async Task AttachEmployeeVoiceAsync(Guid userId, Guid taskId, Guid voiceFileId)
        {
            var task = await _uow.Tasks.GetByIdAsync(taskId) ?? throw new InvalidOperationException("Task not found.");
            if (task.AssignedToUserId != userId)
                throw new UnauthorizedAccessException("Only assignee can attach voice.");
            if (task.StatusId != TaskStatusIds.Open)
                throw new InvalidOperationException("Only Open tasks accept voice commitment.");

            _uow.Begin();
            try
            {
                await _uow.Tasks.AttachVoiceAsync(taskId, voiceFileId);
                await _uow.VoiceFiles.UpdateTaskLinkAsync(voiceFileId, taskId);
                await LogAsync(taskId, userId, "VoiceCommitmentAdded");
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }
        }

        // ---- QUERY ----
        public async Task<TaskDetailDto?> GetDetailAsync(Guid taskId)
        {
            var detail = await _uow.Tasks.GetDetailAsync(taskId);
            if (detail == null) return null;
            detail.Timeline = (await _uow.Timeline.GetByTaskAsync(taskId)).ToList();
            detail.ExtendRequests = (await _uow.ExtendRequests.GetByTaskAsync(taskId)).ToList();
            return detail;
        }

        public Task<IReadOnlyList<TaskListItemDto>> QueryAsync(TaskFilterDto filter) => _uow.Tasks.QueryAsync(filter);

        public async Task<IReadOnlyList<TaskListItemDto>> GetForUserRoleAsync(Guid userId, string role, TaskFilterDto filter)
        {
            switch (role)
            {
                case "Employee":
                    filter.AssignedToUserId ??= userId;
                    return await _uow.Tasks.QueryAsync(filter);
                case "VoiceReviewer":
                    filter.StatusId ??= TaskStatusIds.PendingVoiceReview;
                    return await _uow.Tasks.QueryAsync(filter);
                case "TopManagement":
                case "SystemAdmin":
                default:
                    return await _uow.Tasks.QueryAsync(filter);
            }
        }

        public Task<IReadOnlyList<ExtendRequestDto>> GetPendingExtendRequestsAsync() => _uow.ExtendRequests.GetPendingForManagementAsync();

        public async Task<int> RunOverdueSweepAsync()
        {
            _uow.Begin();
            try
            {
                var affected = await _uow.Tasks.MarkOverdueDueTasksAsync();
                _uow.Commit();
                return affected;
            }
            catch { _uow.Rollback(); throw; }
        }

        public async Task<PerformanceDto> GetSelfPerformanceAsync(Guid userId)
        {
            var filter = new TaskFilterDto { AssignedToUserId = userId };
            var rows = await _uow.Tasks.QueryAsync(filter);
            return BuildPerformance(userId, rows);
        }

        public async Task<IReadOnlyList<PerformanceDto>> GetAllPerformanceAsync()
        {
            var users = await _uow.Users.GetAllAsync();
            var result = new List<PerformanceDto>();
            foreach (var u in users.Where(u => u.RoleName == "Employee"))
            {
                var rows = await _uow.Tasks.QueryAsync(new TaskFilterDto { AssignedToUserId = u.UserId });
                var p = BuildPerformance(u.UserId, rows);
                p.FullName = u.FullName;
                result.Add(p);
            }
            return result;
        }

        private static PerformanceDto BuildPerformance(Guid userId, IReadOnlyList<TaskListItemDto> rows)
        {
            var total = rows.Count;
            var passed = rows.Count(r => r.StatusId == TaskStatusIds.Passed);
            var failed = rows.Count(r => r.StatusId == TaskStatusIds.Failed);
            var cancelled = rows.Count(r => r.StatusId == TaskStatusIds.Cancelled);
            var overdue = rows.Count(r => r.StatusId == TaskStatusIds.Overdue);
            var open = rows.Count(r => r.StatusId == TaskStatusIds.Open || r.StatusId == TaskStatusIds.RequestToExtendRevise);
            var finished = passed + failed;
            return new PerformanceDto
            {
                UserId = userId,
                Total = total,
                Passed = passed,
                Failed = failed,
                Cancelled = cancelled,
                Overdue = overdue,
                Open = open,
                PassRate = finished == 0 ? 0 : Math.Round((double)passed / finished * 100.0, 1)
            };
        }

        public async Task ReopenTaskAsync(Guid userId, Guid taskId)
        {
            var task = await _uow.Tasks.GetByIdAsync(taskId) ?? throw new InvalidOperationException("Task not found.");
            if (task.StatusId != TaskStatusIds.Failed && task.StatusId != TaskStatusIds.Cancelled)
                throw new InvalidOperationException("Only Failed or Cancelled tasks can be reopened.");

            _uow.Begin();
            try
            {
                await _uow.Tasks.UpdateStatusAsync(taskId, TaskStatusIds.Open);
                await LogAsync(taskId, userId, "Reopened", task.StatusId, TaskStatusIds.Open);
                if (task.AssignedToUserId.HasValue)
                    await NotifyAsync(task.AssignedToUserId.Value, taskId, "Task reopened by admin", task.TaskName);
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }
        }

        // ---- notification broadcast helpers ----
        private async Task NotifyManagementAsync(Guid taskId, string title)
        {
            var mgmt = await _uow.Users.GetByRoleAsync("TopManagement");
            if (mgmt.Count == 0) return;
            await _uow.Notifications.CreateManyAsync(mgmt.Select(u => new Notification
            {
                UserId = u.UserId, TaskId = taskId, Title = title, Body = null
            }));
            _realtime.QueueUnreadRefresh(mgmt.Select(u => u.UserId));
        }

        private async Task NotifyReviewersAsync(Guid taskId, string title)
        {
            var rev = await _uow.Users.GetByRoleAsync("VoiceReviewer");
            if (rev.Count == 0) return;
            await _uow.Notifications.CreateManyAsync(rev.Select(u => new Notification
            {
                UserId = u.UserId, TaskId = taskId, Title = title, Body = null
            }));
            _realtime.QueueUnreadRefresh(rev.Select(u => u.UserId));
        }
    }
}