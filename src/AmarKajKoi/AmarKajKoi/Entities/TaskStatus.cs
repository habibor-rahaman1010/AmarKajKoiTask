namespace AmarKajKoi.Entities
{
    public class TaskStatus
    {
        public Guid StatusId { get; set; }
        public string StatusCode { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Well-known status GUIDs. Must match the values inserted by 02_SeedData.sql.
    /// </summary>
    public static class TaskStatusIds
    {
        public static readonly Guid Draft = new("22222222-2222-2222-2222-000000000001");
        public static readonly Guid PendingManagementApproval = new("22222222-2222-2222-2222-000000000002");
        public static readonly Guid PendingVoiceReview = new("22222222-2222-2222-2222-000000000003");
        public static readonly Guid Open = new("22222222-2222-2222-2222-000000000004");
        public static readonly Guid RequestToExtendRevise = new("22222222-2222-2222-2222-000000000005");
        public static readonly Guid Overdue = new("22222222-2222-2222-2222-000000000006");
        public static readonly Guid Passed = new("22222222-2222-2222-2222-000000000007");
        public static readonly Guid Failed = new("22222222-2222-2222-2222-000000000008");
        public static readonly Guid Cancelled = new("22222222-2222-2222-2222-000000000009");
    }
}