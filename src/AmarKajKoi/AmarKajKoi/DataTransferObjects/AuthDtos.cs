namespace AmarKajKoi.DataTransferObjects
{
    public record LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public record LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserProfileDto User { get; set; } = new();
    }

    public record UserProfileDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public Guid RoleId { get; set; }
    }

    public record RegisterUserRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string RoleName { get; set; } = "Employee";
    }
}
