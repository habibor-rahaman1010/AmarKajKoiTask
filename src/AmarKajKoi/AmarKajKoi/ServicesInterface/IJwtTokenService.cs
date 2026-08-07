using AmarKajKoi.Entities;

namespace AmarKajKoi.ServicesInterface
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user);
    }
}
