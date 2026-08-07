using AmarKajKoi.Database;
using AmarKajKoi.Entities;
using AmarKajKoi.RepositoriesInterface;
using Dapper;

namespace AmarKajKoi.RepositoriesImplement
{
    public class UserRepository : IUserRepository
    {
        private readonly IUnitOfWork _uow;

        public UserRepository(IUnitOfWork uow) 
        {
            _uow = uow; 
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            const string sql = @"
                SELECT u.UserId, u.FullName, u.Email, u.Username, u.PasswordHash, u.RoleId,
                       u.IsActive, u.CreatedAt, u.UpdatedAt, r.RoleName
                FROM dbo.Users u
                INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
                WHERE u.Username = @username;";
            return await _uow.Connection.QueryFirstOrDefaultAsync<User>(sql, new { username }, _uow.Transaction);
        }

        public async Task<User?> GetByIdAsync(Guid userId)
        {
            const string sql = @"
                SELECT u.UserId, u.FullName, u.Email, u.Username, u.PasswordHash, u.RoleId,
                       u.IsActive, u.CreatedAt, u.UpdatedAt, r.RoleName
                FROM dbo.Users u
                INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
                WHERE u.UserId = @userId;";
            return await _uow.Connection.QueryFirstOrDefaultAsync<User>(sql, new { userId }, _uow.Transaction);
        }

        public async Task<Guid> CreateAsync(User user)
        {
            const string sql = @"
                INSERT INTO dbo.Users(FullName, Email, Username, PasswordHash, RoleId, IsActive)
                OUTPUT INSERTED.UserId
                VALUES (@FullName, @Email, @Username, @PasswordHash, @RoleId, 1);";
            return await _uow.Connection.ExecuteScalarAsync<Guid>(sql, user, _uow.Transaction);
        }

        public async Task UpdatePasswordAsync(Guid userId, string passwordHash)
        {
            const string sql = @"UPDATE dbo.Users SET PasswordHash=@passwordHash, UpdatedAt=SYSUTCDATETIME() WHERE UserId=@userId;";
            await _uow.Connection.ExecuteAsync(sql, new { userId, passwordHash }, _uow.Transaction);
        }

        public async Task<IReadOnlyList<User>> GetAllAsync()
        {
            const string sql = @"
                SELECT u.UserId, u.FullName, u.Email, u.Username, u.PasswordHash, u.RoleId,
                       u.IsActive, u.CreatedAt, u.UpdatedAt, r.RoleName
                FROM dbo.Users u
                INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
                ORDER BY u.FullName;";
            var rows = await _uow.Connection.QueryAsync<User>(sql, transaction: _uow.Transaction);
            return rows.ToList();
        }

        public async Task<IReadOnlyList<User>> GetByRoleAsync(string roleName)
        {
            const string sql = @"
                SELECT u.UserId, u.FullName, u.Email, u.Username, u.PasswordHash, u.RoleId,
                       u.IsActive, u.CreatedAt, u.UpdatedAt, r.RoleName
                FROM dbo.Users u
                INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
                WHERE r.RoleName = @roleName AND u.IsActive = 1
                ORDER BY u.FullName;";
            var rows = await _uow.Connection.QueryAsync<User>(sql, new { roleName }, _uow.Transaction);
            return rows.ToList();
        }

        public async Task<Guid?> GetRoleIdByNameAsync(string roleName)
        {
            const string sql = "SELECT RoleId FROM dbo.Roles WHERE RoleName = @roleName;";
            return await _uow.Connection.ExecuteScalarAsync<Guid?>(sql, new { roleName }, _uow.Transaction);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            const string sql = @"
                SELECT u.UserId, u.FullName, u.Email, u.Username, u.PasswordHash, u.RoleId,
                       u.IsActive, u.CreatedAt, u.UpdatedAt, r.RoleName
                FROM dbo.Users u
                INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
                WHERE u.Email = @email";

            return await _uow.Connection.QueryFirstOrDefaultAsync<User>(sql, new { email }, _uow.Transaction);
        }
    }
}