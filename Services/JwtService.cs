using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WebFashion.Data;
using WebFashion.Models;

namespace WebFashion.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        Task<string> GenerateRefreshToken(int userId);
    }

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        public JwtService(IConfiguration configuration , ApplicationDbContext context)
        {
            _context = context;
            _configuration = configuration;
        }

        public static string Hash256(string input)
        {
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input)); 
            string hash = Convert.ToBase64String(hashBytes);
            return hash;
        }

        public static string GenerateRandomString(int length)
        {
            string s = "";
            var random = new Random();
            for (int i = 0; i < length; i++)
            {
                s += (char)random.Next('a','z');
            }
            return s;
        }
        public string GenerateToken(User user)
        {
            var secretKey = _configuration["Jwt:SecretKey"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60M");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var roleName = user.Role switch
            {
                UserRole.Admin => "Admin",
                UserRole.User => "User",
             
            };

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("UserId", user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role, roleName) // 🔹 "Admin" hoặc "Customer"
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string> GenerateRefreshToken(int userId)
        {
            string refreshToken = GenerateRandomString(64);
            string hashRefreshToken = Hash256((refreshToken + userId));
            var data = new RefreshToken
            {
                UserId = userId,
                Token = hashRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                Revoked = false
            };
            _context.RefreshTokens.Add(data);   
            
            await _context.SaveChangesAsync();  
            
            return hashRefreshToken;
        }
    }
}
