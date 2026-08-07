namespace AmarKajKoi.Entities
{
    public class TaskCenter
    {
        public Guid TaskCenterId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
