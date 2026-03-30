using Microsoft.EntityFrameworkCore;
using ExpenseTracker.API.Data;
using ExpenseTracker.API.Models;
using ExpenseTracker.API.DTOs.User;

namespace ExpenseTracker.API.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;

        public UserService(AppDbContext context, AuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        public async Task<string?> UpdateProfileAsync(int userId, UpdateProfileDto model)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            if (user.Email != model.Email)
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
                if (emailExists) return null;
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            await _context.SaveChangesAsync();

            var updatedUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (updatedUser == null) return null;

            return _authService.GenerateJwtToken(updatedUser);
        }
        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto model)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            bool isPasswordValid = PasswordHasherHelper.VerifyPassword(model.OldPassword, user.PasswordHash);
            if (!isPasswordValid) return false;

            user.PasswordHash = PasswordHasherHelper.HashPassword(model.NewPassword);

            await _context.SaveChangesAsync();
            return true;
        }
    }
}