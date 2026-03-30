using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExpenseTracker.API.DTOs.User;
using ExpenseTracker.API.Services;
using System.Security.Claims;

namespace ExpenseTracker.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("nameid")?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized(new { message = "Không tìm thấy thông tin định danh người dùng trong Token!" });

            var userId = int.Parse(userIdStr);

            var newToken = await _userService.UpdateProfileAsync(userId, model);

            if (string.IsNullOrEmpty(newToken))
                return BadRequest(new { message = "Cập nhật thất bại hoặc Email đã tồn tại!" });

            return Ok(new { message = "Cập nhật thành công!", token = newToken });
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("nameid")?.Value
                            ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            var success = await _userService.ChangePasswordAsync(userId, model);

            if (!success)
            {
                return BadRequest(new { message = "Mật khẩu cũ không chính xác hoặc đổi mật khẩu thất bại!" });
            }

            return Ok(new { message = "Đổi mật khẩu thành công!" });
        }
    }
}