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
        public AuthController(AuthService authService) => _authService = authService;

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            var user = await _authService.Register(model);
            if (user == null) return BadRequest("Email đã tồn tại!");
            return Ok(new { message = "Đăng ký thành công" });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto model)
        {
            // Logic: Kiểm tra Password -> Gọi JwtHelper.GenerateToken()
            // Tạm thời trả về token giả để bạn test luồng Mobile
            return Ok(new { token = "fake-jwt-token", email = model.Email });
        }
    }
}