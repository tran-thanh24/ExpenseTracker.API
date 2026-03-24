using Microsoft.AspNetCore.Mvc;
using ExpenseTracker.API.DTOs.Auth;
using ExpenseTracker.API.Services;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            var user = await _authService.RegisterAsync(model);
            if (user == null) return BadRequest(new { message = "Email đã tồn tại!" });
            return Ok(new { message = "Đăng ký thành công" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var user = await _authService.LoginAsync(model);
            if (user == null) return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });

            return Ok(new
            {
                token = "fake-jwt-token-123",
                username = user.FullName,
                email = user.Email
            });
        }
    }
}