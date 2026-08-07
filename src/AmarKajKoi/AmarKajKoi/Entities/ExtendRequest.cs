namespace AmarKajKoi.Entities
{
    public class ExtendRequest
    {
        public Guid RequestId { get; set; }
        public Guid TaskId { get; set; }
        public Guid RequestedByUserId { get; set; }
        public string RequestType { get; set; } = "Extend"; // Extend | Revise
        public DateTime? RequestedDueDate { get; set; }
        public string? ReasonText { get; set; }
        public Guid? VoiceFileId { get; set; }
        public string Status { get; set; } = "Pending";
        public Guid? DecisionByUserId { get; set; }
        public string? DecisionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DecidedAt { get; set; }
    }
}
