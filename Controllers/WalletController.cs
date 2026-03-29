using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ExpenseTracker.API.Data;
using ExpenseTracker.API.Models;
using System.Security.Claims;

namespace ExpenseTracker.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WalletController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WalletController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);
            var wallets = await _context.Wallets.Where(w => w.UserId == userId).ToListAsync();
            return Ok(wallets);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Wallet wallet)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null) return Unauthorized();

            wallet.UserId = int.Parse(userIdClaim);

            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();
            return Ok(wallet);
        }
    }
}