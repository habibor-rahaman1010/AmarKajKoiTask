namespace AmarKajKoi.Entities
{
    public class DayEvent
    {
        public Guid DayEventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid? EventChannelId { get; set; }
        public DateTime? EventDate { get; set; }
        public bool IsActive { get; set; }
    }
}
