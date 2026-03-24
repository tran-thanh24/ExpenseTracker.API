using ExpenseTracker.API.DTOs.Auth;
using ExpenseTracker.API.Models;
using ExpenseTracker.API.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.API.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        public AuthService(AppDbContext context) => _context = context;

        public async Task<Users?> Register(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email)) return null;

            var user = new Users
            {
                FullName = dto.FullName,
                Email = dto.Email,
                // Trong thực tế hãy sử dụng BCrypt để Hash Password
                PasswordHash = dto.Password
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
    }
}