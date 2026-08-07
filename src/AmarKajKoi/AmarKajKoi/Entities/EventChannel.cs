namespace AmarKajKoi.Entities
{
    public class EventChannel
    {
        public Guid EventChannelId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
