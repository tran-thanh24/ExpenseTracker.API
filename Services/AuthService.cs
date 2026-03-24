using ExpenseTracker.API.Data;
using ExpenseTracker.API.DTOs.Auth;
using ExpenseTracker.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.API.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        // Logic Đăng ký
        public async Task<Users?> RegisterAsync(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return null; // Email đã tồn tại

            var user = new Users
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = dto.Password // Lưu ý: Sau này nên Hash mật khẩu
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        // Logic Đăng nhập
        public async Task<Users?> LoginAsync(LoginDto dto)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.PasswordHash == dto.Password);
        }
    }
}