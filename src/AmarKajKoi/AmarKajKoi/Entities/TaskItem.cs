namespace AmarKajKoi.Entities
{
    public class TaskItem
    {
        public Guid TaskId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty; // Target | Commitment
        public Guid? TaskCenterId { get; set; }
        public Guid? EventChannelId { get; set; }
        public Guid? DayEventId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid StatusId { get; set; }
        public int RequestCount { get; set; }
        public bool IsPinned { get; set; }
        public Guid? VoiceFileId { get; set; }
        public string? VoiceAutoText { get; set; }
        public string? Description { get; set; }
        public string? FinalComment { get; set; }
        public byte[]? RowVersion { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
