using AmarKajKoi.DataTransferObjects;

namespace AmarKajKoi.ServicesInterface
{
    public interface IAuthService
    {
        Task<LoginResponse?> LoginAsync(LoginRequest request);
        Task<UserProfileDto> RegisterAsync(RegisterUserRequest request);
        Task<UserProfileDto?> GetProfileAsync(Guid userId);
    }
}
