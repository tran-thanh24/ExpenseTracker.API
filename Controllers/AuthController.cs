using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Thêm cái này để dùng .Where, .Select
using ExpenseTracker.API.Data; // Thêm cái này để dùng AppDbContext
using ExpenseTracker.API.DTOs.Auth;
using ExpenseTracker.API.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly AppDbContext _context;

        public AuthController(AuthService authService, AppDbContext context)
        {
            _authService = authService;
            _context = context;
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
            var token = await _authService.LoginAsync(model);

            if (token == null)
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });

            return Ok(new
            {
                token = token,
                message = "Đăng nhập thành công"
            });
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var user = await _context.Users
                .Where(u => u.Email == email)
                .Select(u => new { u.FullName, u.Email })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound();

            return Ok(user);
        }
    }
}