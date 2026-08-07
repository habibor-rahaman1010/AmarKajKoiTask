namespace AmarKajKoi.Entities
{
    public class Notification
    {
        public Guid NotificationId { get; set; }
        public Guid UserId { get; set; }
        public Guid? TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Body { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
