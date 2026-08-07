namespace AmarKajKoi.DataTransferObjects
{
    public record TargetVoiceCreateDto
    {
        public string TaskName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? VoiceFileId { get; set; }
        public bool PostImmediately { get; set; } = true;
    }

    public record CommitmentFormCreateDto
    {
        public string TaskName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? TaskCenterId { get; set; }
        public Guid? EventChannelId { get; set; }
        public Guid? DayEventId { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid? VoiceFileId { get; set; }
        public bool PostImmediately { get; set; } = true;
    }

    public record CommitmentEditDto
    {
        public Guid TaskId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? TaskCenterId { get; set; }
        public Guid? EventChannelId { get; set; }
        public Guid? DayEventId { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public record TargetEditDto
    {
        public Guid TaskId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? VoiceFileId { get; set; }
    }

    public record VoiceReviewCompleteDto
    {
        public Guid TaskId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public Guid TaskCenterId { get; set; }
        public Guid EventChannelId { get; set; }
        public Guid? DayEventId { get; set; }
        public DateTime DueDate { get; set; }
        public Guid AssignedToUserId { get; set; }
    }

    public record SendBackDto
    {
        public Guid TaskId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public record ApproveDto
    {
        public Guid TaskId { get; set; }
        public Guid? AssignedToUserId { get; set; }
    }

    public record RejectDto
    {
        public Guid TaskId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public record ExtendRequestCreateDto
    {
        public Guid TaskId { get; set; }
        public string RequestType { get; set; } = "Extend"; // Extend | Revise
        public DateTime? RequestedDueDate { get; set; }
        public string? ReasonText { get; set; }
        public Guid? VoiceFileId { get; set; }
    }

    public record ExtendRequestDecisionDto
    {
        public Guid RequestId { get; set; }
        public bool Approve { get; set; }
        public string? Reason { get; set; }
    }

    public record ChangeDueDateDto
    {
        public Guid TaskId { get; set; }
        public DateTime NewDueDate { get; set; }
        public string? Note { get; set; }
    }

    public record ChangeAssigneeDto
    {
        public Guid TaskId { get; set; }
        public Guid NewAssigneeUserId { get; set; }
    }

    public record MarkFinalDto
    {
        public Guid TaskId { get; set; }
        public string Decision { get; set; } = string.Empty; // Passed | Failed | Cancelled
        public string? Comment { get; set; }
    }

    public record BulkFinalDto
    {
        public Guid[] TaskIds { get; set; } = Array.Empty<Guid>();
        public string Decision { get; set; } = string.Empty;
        public string? Comment { get; set; }
    }

    public record RequestMarkPassedDto
    {
        public Guid TaskId { get; set; }
        public string? Note { get; set; }
    }

    public record TaskListItemDto
    {
        public Guid TaskId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty;
        public string? TaskCenter { get; set; }
        public string? EventChannel { get; set; }
        public string? DayEvent { get; set; }
        public string? AssignedToName { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? CreatedByName { get; set; }
        public Guid CreatedByUserId { get; set; }
        public DateTime? DueDate { get; set; }
        public string StatusCode { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public Guid StatusId { get; set; }
        public int RequestCount { get; set; }
        public bool IsPinned { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public record TaskDetailDto : TaskListItemDto
    {
        public string? Description { get; set; }
        public string? VoiceAutoText { get; set; }
        public Guid? VoiceFileId { get; set; }
        public string? FinalComment { get; set; }
        public List<TimelineDto> Timeline { get; set; } = new();
        public List<ExtendRequestDto> ExtendRequests { get; set; } = new();
    }

    public record TimelineDto
    {
        public Guid TimelineId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string ActionByName { get; set; } = string.Empty;
        public string? FromStatus { get; set; }
        public string? ToStatus { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public record ExtendRequestDto
    {
        public Guid RequestId { get; set; }
        public Guid TaskId { get; set; }
        public string RequestedByName { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public DateTime? RequestedDueDate { get; set; }
        public string? ReasonText { get; set; }
        public Guid? VoiceFileId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? DecisionByName { get; set; }
        public string? DecisionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DecidedAt { get; set; }
    }

    public record TaskFilterDto
    {
        public Guid? StatusId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public Guid? EventChannelId { get; set; }
        public string? Search { get; set; }
    }

    public record NotificationDto
    {
        public Guid NotificationId { get; set; }
        public Guid? TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Body { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public record PerformanceDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Cancelled { get; set; }
        public int Overdue { get; set; }
        public int Open { get; set; }
        public double PassRate { get; set; }
    }

    public record VoiceUploadResponse
    {
        public Guid VoiceFileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int DurationSecs { get; set; }
    }
}
