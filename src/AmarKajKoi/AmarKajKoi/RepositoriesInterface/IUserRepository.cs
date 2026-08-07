using AmarKajKoi.Entities;

namespace AmarKajKoi.RepositoriesInterface
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(Guid userId);
        Task<Guid> CreateAsync(User user);
        Task UpdatePasswordAsync(Guid userId, string passwordHash);
        Task<IReadOnlyList<User>> GetAllAsync();
        Task<IReadOnlyList<User>> GetByRoleAsync(string roleName);
        Task<Guid?> GetRoleIdByNameAsync(string roleName);
    }
}
