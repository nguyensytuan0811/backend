using WebFashion.Data;
using WebFashion.DTOs;
using WebFashion.Models;
using Microsoft.EntityFrameworkCore;

namespace WebFashion.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<UserDto> RegisterAsync(RegisterRequest request);
        Task<User> GetUserByEmailAsync(string email);

        Task DeleteOldRefreshTokenAsync(int userId);

        Task DeleteOldRefreshTokenAsync(string refreshToken);

        Task<User?> GetUserByRefreshTokenAsync(string refreshToken);


    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;

        public AuthService(ApplicationDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);
            
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            var accesstoken =  _jwtService.GenerateToken(user);
            var refreshToken = await _jwtService.GenerateRefreshToken(user.Id);
            return new LoginResponse
            {
                Token = accesstoken,
                UserId = user.Id,
                UserRole = (int)user.Role,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                RefreshToken = refreshToken
            };
        }
        public async Task<UserDto> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email already exists");
            }

            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                UpdatedAt = DateTime.UtcNow,
                Role = UserRole.User
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = (int)user.Role,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        }
        
        public async Task<User?> GetUserByRefreshTokenAsync(string refreshToken)
        {
            var user = await _context.RefreshTokens.
                Where(x => x.Token == refreshToken && (x.Revoked == false) && x.ExpiresAt > DateTime.Now)
                .Select(x => x.User)
                .FirstOrDefaultAsync();
            return user;
        }

        public async Task DeleteOldRefreshTokenAsync(string refreshToken)
        {
            var entity = await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == refreshToken);
            if (entity == null)
            {
                return;
            }
            _context.RefreshTokens.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteOldRefreshTokenAsync(int userId)
        {
            var entity = await _context.RefreshTokens
                .Where(x => x.UserId == userId)
                .ToListAsync();

            if (entity != null)
            {
                return;
            }
            _context.RefreshTokens.RemoveRange(entity);
            await _context.SaveChangesAsync();
        }
    }
}
