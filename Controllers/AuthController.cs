using Microsoft.AspNetCore.Mvc;
using WebFashion.DTOs;
using WebFashion.Services;

namespace WebFashion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtService _jwtService;

        public AuthController(IAuthService authService , IJwtService jwtService)
        {
            _authService = authService;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                var newOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false,
                    SameSite = SameSiteMode.None,
                };
                var fullname = response.FirstName + " " + response.LastName;
                HttpContext.Response.Cookies.Append("refreshToken", response.RefreshToken, newOptions);
                return Ok(new {response.Token , response.UserRole , fullname , response.UserId});
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var user = await _authService.RegisterAsync(request);
                return CreatedAtAction(nameof(Register), user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _authService.DeleteOldRefreshTokenAsync(refreshToken);
            }
            // Phải truyền lại options giống khi tạo cookie
            var options = new CookieOptions
            {
                Secure = true,
                SameSite = SameSiteMode.None,
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(-1) // ép hết hạn
            };
            HttpContext.Session.Clear();
            HttpContext.Response.Cookies.Delete("refreshToken", options);
            return Ok();        
        }
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var isExist = HttpContext.Request.Cookies.TryGetValue("refreshToken", out var refreshToken);
            if (!isExist)
            {
                return Unauthorized("RefreshToken is not found");
            }
            var user = await _authService.GetUserByRefreshTokenAsync(refreshToken!);
            if (user == null)
            {
                return Unauthorized("RefreshToken is not found");
            }

            await _authService.DeleteOldRefreshTokenAsync(user.Id);

            var newOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
            };
            var accessToken =  _jwtService.GenerateToken(user);
            var mew_refreshToken = await _jwtService.GenerateRefreshToken(user.Id);
            HttpContext.Response.Cookies.Append("refreshToken", mew_refreshToken, newOptions);
            return Ok(accessToken);
        }
    }
}
