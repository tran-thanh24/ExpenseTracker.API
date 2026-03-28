using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ExpenseTracker.API.Data;
using ExpenseTracker.API.Models;

namespace ExpenseTracker.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ExpenseController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ExpenseController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var data = await _context.Expenses
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Expense expense)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            int userId = int.Parse(userIdStr);

            expense.UserId = userId;
            if (expense.Date == default) expense.Date = DateTime.Now;

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == expense.WalletId && w.UserId == userId);
            if (wallet == null) return BadRequest("Ví không hợp lệ.");

            wallet.Balance += expense.Amount;

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Giao dịch thành công",
                NewBalance = wallet.Balance,
                Data = expense
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExpense(int id, [FromBody] Expense expense)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var dbExpense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (dbExpense == null) return NotFound("Không tìm thấy giao dịch");

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == dbExpense.WalletId && w.UserId == userId);
            if (wallet != null)
            {
                wallet.Balance = (wallet.Balance + dbExpense.Amount) - expense.Amount;
            }

            dbExpense.Title = expense.Title;
            dbExpense.Amount = expense.Amount;
            dbExpense.Category = expense.Category;
            dbExpense.Date = expense.Date;
            dbExpense.WalletId = expense.WalletId;

            await _context.SaveChangesAsync();
            return Ok(dbExpense);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
            if (expense == null) return NotFound();

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == expense.WalletId && w.UserId == userId);
            if (wallet != null) wallet.Balance += expense.Amount;

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa và hoàn tiền thành công" });
        }

        [HttpGet("wallet/{walletId}")]
        public async Task<ActionResult> GetExpensesByWallet(int walletId)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int userId = int.Parse(userIdClaim.Value);

            var expenses = await _context.Expenses
                .Where(e => e.WalletId == walletId)
                .OrderByDescending(e => e.Date)
                .Select(e => new
                {
                    e.Id,
                    e.Amount,
                    e.Title,
                    e.Category,
                    Date = e.Date.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();

            return Ok(expenses);
        }
    }
}