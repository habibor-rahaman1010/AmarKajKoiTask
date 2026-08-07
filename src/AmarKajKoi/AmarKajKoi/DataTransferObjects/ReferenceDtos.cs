namespace AmarKajKoi.DataTransferObjects
{
    /// <summary>
    /// A user as the reference endpoints expose them: enough to fill an assignee
    /// dropdown or a user list, and nothing more. The User entity must not be
    /// returned directly — it carries PasswordHash, which would then be readable
    /// by every authenticated caller.
    /// </summary>
    public record UserRefDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }
}
