namespace AmarKajKoi.Entities
{
    public class TaskTimeline
    {
        public Guid TimelineId { get; set; }
        public Guid TaskId { get; set; }
        public Guid ActionByUserId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public Guid? FromStatusId { get; set; }
        public Guid? ToStatusId { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
