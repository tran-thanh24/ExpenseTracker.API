using ExpenseTracker.API.Data;
using ExpenseTracker.API.DTOs.Auth;
using ExpenseTracker.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens; // Thêm dòng này
using System.IdentityModel.Tokens.Jwt; // Thêm dòng này
using System.Security.Claims; // Thêm dòng này
using System.Text; // Thêm dòng này

namespace ExpenseTracker.API.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<Users?> RegisterAsync(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return null;

            var user = new Users
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = dto.Password // Lưu ý: Sau này nên dùng BCrypt để Hash mật khẩu
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<string?> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.PasswordHash == dto.Password);

            if (user == null) return null;

            // TẠO TOKEN THẬT TẠI ĐÂY
            return GenerateJwtToken(user);
        }

        private string GenerateJwtToken(Users user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            // Key này PHẢI trùng với key trong Program.cs
            var key = Encoding.UTF8.GetBytes("Chuoi_Key_Bi_Mat_Cua_Thanh_2026_Sieu_Dai");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.FullName)
                }),
                Expires = DateTime.UtcNow.AddDays(7), // Token hết hạn sau 7 ngày
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}