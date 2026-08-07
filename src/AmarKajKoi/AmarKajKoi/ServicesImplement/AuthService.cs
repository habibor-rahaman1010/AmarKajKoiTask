using AmarKajKoi.Database;
using AmarKajKoi.DataTransferObjects;
using AmarKajKoi.Entities;
using AmarKajKoi.ServicesInterface;

namespace AmarKajKoi.ServicesImplement
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly IPasswordHasher _hasher;
        private readonly IJwtTokenService _jwt;

        public AuthService(IUnitOfWork uow, IPasswordHasher hasher, IJwtTokenService jwt)
        {
            _uow = uow;
            _hasher = hasher;
            _jwt = jwt;
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _uow.Users.GetByUsernameAsync(request.Username);
            if (user == null || !user.IsActive)
            {
                return null;
            }

            if (!_hasher.Verify(request.Password, user.PasswordHash))
            {
                return null;
            }

            if (user.PasswordHash.StartsWith("PLAIN:", StringComparison.Ordinal))
            {
                _uow.Begin();
                try
                {
                    await _uow.Users.UpdatePasswordAsync(user.UserId, _hasher.Hash(request.Password));
                    _uow.Commit();
                }
                catch 
                { 
                    _uow.Rollback(); 
                    throw; 
                }
            }

            var token = _jwt.GenerateToken(user);
            return new LoginResponse
            {
                Token = token,
                User = new UserProfileDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Username = user.Username,
                    RoleName = user.RoleName ?? string.Empty,
                    RoleId = user.RoleId
                }
            };
        }

        public async Task<UserProfileDto> RegisterAsync(RegisterUserRequest request)
        {
            var roleId = await _uow.Users.GetRoleIdByNameAsync(request.RoleName)
                ?? throw new InvalidOperationException($"Role '{request.RoleName}' not found.");

            var existing = await _uow.Users.GetByUsernameAsync(request.Username);
            if (existing != null)
            {
                throw new InvalidOperationException($"Username '{request.Username}' already exists.");
            }

            var emailExist = await _uow.Users.GetByEmailAsync(request.Email);
            if (emailExist != null)
            {
                throw new InvalidOperationException($"Email '{request.Email}' already exists.");
            }

            var user = new User
            {
                FullName = request.FullName,
                Username = request.Username,
                Email = request.Email,
                PasswordHash = _hasher.Hash(request.Password),
                RoleId = roleId
            };

            _uow.Begin();
            try
            {
                user.UserId = await _uow.Users.CreateAsync(user);
                _uow.Commit();
            }
            catch { _uow.Rollback(); throw; }

            var saved = await _uow.Users.GetByIdAsync(user.UserId);
            return new UserProfileDto
            {
                UserId = saved!.UserId,
                FullName = saved.FullName,
                Email = saved.Email,
                Username = saved.Username,
                RoleName = saved.RoleName ?? string.Empty,
                RoleId = saved.RoleId
            };
        }

        public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
        {
            var u = await _uow.Users.GetByIdAsync(userId);
            if (u == null)
            {
                return null;
            }
            return new UserProfileDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                Username = u.Username,
                RoleName = u.RoleName ?? string.Empty,
                RoleId = u.RoleId
            };
        }
    }
}
