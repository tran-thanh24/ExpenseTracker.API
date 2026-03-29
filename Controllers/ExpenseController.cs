using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ExpenseTracker.API.Data;
using ExpenseTracker.API.Models;
using ExpenseTracker.API.DTOs.Statistics;

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

        [HttpGet("statistics/{type}")]
        public async Task<ActionResult<StatisticsDto>> GetStatistics(string type, [FromQuery] DateTime? customDate = null)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            DateTime now = DateTime.Today;
            DateTime startDate;

            DateTime endDate = now.AddDays(1);

            switch (type.ToLower())
            {
                case "day":
                    startDate = now.Date;
                    break;
                case "week":
                    startDate = now.Date.AddDays(-7);
                    break;
                case "month":
                    startDate = new DateTime(now.Year, now.Month, 1);
                    break;
                case "year":
                    startDate = new DateTime(now.Year, 1, 1);
                    break;
                case "custom":
                    if (customDate == null) return BadRequest("Thiếu ngày tùy chọn.");
                    DateTime localCustomDate = customDate.Value.ToLocalTime();
                    startDate = localCustomDate.Date;
                    endDate = startDate.AddDays(1);
                    break;
                default:
                    return BadRequest("Tham số không hợp lệ. Chỉ chấp nhận: day, week, month, year, custom.");
            }

            var data = await _context.Expenses
                .Where(e => e.UserId == userId && e.Date >= startDate && e.Date < endDate)
                .ToListAsync();

            var response = new StatisticsDto
            {
                TotalIncome = data.Where(e => e.Kind == TransactionKind.Income).Sum(e => e.Amount),
                TotalExpense = data.Where(e => e.Kind == TransactionKind.Expense).Sum(e => e.Amount),

                CategoryData = data.Where(e => e.Kind == TransactionKind.Expense)
                    .GroupBy(e => !string.IsNullOrEmpty(e.Category) ? e.Category : (!string.IsNullOrEmpty(e.Title) ? e.Title : "Khác"))
                    .Select(g => new CategoryStat
                    {
                        Name = g.Key,
                        Amount = g.Sum(e => e.Amount)
                    })
                    .OrderByDescending(x => x.Amount)
                    .ToList(),

                IncomeCategoryData = data.Where(e => e.Kind == TransactionKind.Income)
                    .GroupBy(e => !string.IsNullOrEmpty(e.Category) ? e.Category : (!string.IsNullOrEmpty(e.Title) ? e.Title : "Khác"))
                    .Select(g => new CategoryStat
                    {
                        Name = g.Key,
                        Amount = g.Sum(e => e.Amount)
                    })
                    .OrderByDescending(x => x.Amount)
                    .ToList()
            };

            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);
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

            if (expense.Amount <= 0)
                return BadRequest("Số tiền phải lớn hơn 0.");

            expense.UserId = userId;
            if (expense.Date == default) expense.Date = DateTime.Now;

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == expense.WalletId && w.UserId == userId);
            if (wallet == null) return BadRequest("Ví không hợp lệ.");

            wallet.Balance += expense.Kind.ToWalletDelta(expense.Amount);

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
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            if (expense.Amount <= 0)
                return BadRequest("Số tiền phải lớn hơn 0.");

            var dbExpense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (dbExpense == null) return NotFound("Không tìm thấy giao dịch");

            var oldDelta = dbExpense.Kind.ToWalletDelta(dbExpense.Amount);
            var newDelta = expense.Kind.ToWalletDelta(expense.Amount);

            if (dbExpense.WalletId == expense.WalletId)
            {
                var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == dbExpense.WalletId && w.UserId == userId);
                if (wallet == null) return BadRequest("Ví không hợp lệ.");
                wallet.Balance = wallet.Balance - oldDelta + newDelta;
            }
            else
            {
                var oldWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == dbExpense.WalletId && w.UserId == userId);
                var newWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == expense.WalletId && w.UserId == userId);
                if (oldWallet == null || newWallet == null) return BadRequest("Ví không hợp lệ.");

                oldWallet.Balance -= oldDelta;
                newWallet.Balance += newDelta;
            }

            dbExpense.Title = expense.Title;
            dbExpense.Amount = expense.Amount;
            dbExpense.Category = expense.Category;
            dbExpense.Date = expense.Date;
            dbExpense.WalletId = expense.WalletId;
            dbExpense.Kind = expense.Kind;

            await _context.SaveChangesAsync();
            return Ok(dbExpense);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);
            var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
            if (expense == null) return NotFound();

            var delta = expense.Kind.ToWalletDelta(expense.Amount);
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == expense.WalletId && w.UserId == userId);
            if (wallet != null) wallet.Balance -= delta;

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa và hoàn tiền thành công" });
        }

        [HttpGet("wallet/{walletId}")]
        public async Task<ActionResult> GetExpensesByWallet(int walletId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int userId = int.Parse(userIdClaim.Value);

            var ownsWallet = await _context.Wallets.AnyAsync(w => w.Id == walletId && w.UserId == userId);
            if (!ownsWallet) return NotFound();

            var expenses = await _context.Expenses
                .Where(e => e.WalletId == walletId && e.UserId == userId)
                .OrderByDescending(e => e.Date)
                .Select(e => new
                {
                    e.Id,
                    e.Amount,
                    e.Title,
                    e.Category,
                    Kind = e.Kind,
                    Date = e.Date.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();

            return Ok(expenses);
        }
    }
}