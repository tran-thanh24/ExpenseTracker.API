using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExpenseTracker.API.Data;
using ExpenseTracker.API.Models;
using ExpenseTracker.API.DTOs;
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

            DateTime baseDate = customDate.HasValue ? customDate.Value.ToLocalTime() : DateTime.Today;
            DateTime startDate;
            DateTime endDate;

            switch (type.ToLower())
            {
                case "day":
                    startDate = baseDate.Date;
                    endDate = startDate.AddDays(1);
                    break;
                case "week":
                    endDate = baseDate.Date.AddDays(1);
                    startDate = endDate.AddDays(-7);
                    break;
                case "month":
                    startDate = new DateTime(baseDate.Year, baseDate.Month, 1);
                    endDate = startDate.AddMonths(1);
                    break;
                case "year":
                    startDate = new DateTime(baseDate.Year, 1, 1);
                    endDate = startDate.AddYears(1);
                    break;
                case "custom":
                    if (customDate == null) return BadRequest("Thiếu ngày tùy chọn.");
                    startDate = baseDate.Date;
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
                    .GroupBy(e => !string.IsNullOrEmpty(e.Category) ? e.Category : (!string.IsNullOrEmpty(e.Title) ? e.Title : "Chi tiêu khác"))
                    .Select(g => new CategoryStat
                    {
                        Name = g.Key,
                        Amount = g.Sum(e => e.Amount)
                    })
                    .OrderByDescending(x => x.Amount)
                    .ToList(),

                IncomeCategoryData = data.Where(e => e.Kind == TransactionKind.Income)
                    .GroupBy(e => !string.IsNullOrEmpty(e.Category) ? e.Category : (!string.IsNullOrEmpty(e.Title) ? e.Title : "Thu nhập khác"))
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
        public async Task<IActionResult> Create([FromBody] ExpenseUpsertDto request)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            int userId = int.Parse(userIdStr);

            if (request.Amount <= 0)
                return BadRequest("Số tiền phải lớn hơn 0.");
            if (!request.Kind.HasValue)
                return BadRequest("Thiếu loại giao dịch (kind). Vui lòng gửi Expense hoặc Income.");

            var expense = new Expense
            {
                Title = request.Title,
                Amount = Math.Abs(request.Amount),
                Category = request.Category,
                Date = request.Date == default ? DateTime.Now : request.Date,
                WalletId = request.WalletId,
                Kind = request.Kind.Value,
                UserId = userId
            };

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == expense.WalletId && w.UserId == userId);
            if (wallet == null) return BadRequest("Ví không hợp lệ.");

            if (expense.Kind == TransactionKind.Expense && wallet.Balance < expense.Amount)
                return BadRequest(new { message = "Vượt quá số dư ví." });

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
        public async Task<IActionResult> UpdateExpense(int id, [FromBody] ExpenseUpsertDto request)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = int.Parse(userIdStr);

            if (request.Amount <= 0)
                return BadRequest("Số tiền phải lớn hơn 0.");
            if (!request.Kind.HasValue)
                return BadRequest("Thiếu loại giao dịch (kind). Vui lòng gửi Expense hoặc Income.");

            var dbExpense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (dbExpense == null) return NotFound("Không tìm thấy giao dịch");

            var normalizedAmount = Math.Abs(request.Amount);
            var newKind = request.Kind.Value;

            var oldDelta = dbExpense.Kind.ToWalletDelta(dbExpense.Amount);
            var newDelta = newKind.ToWalletDelta(normalizedAmount);

            if (dbExpense.WalletId == request.WalletId)
            {
                var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == dbExpense.WalletId && w.UserId == userId);
                if (wallet == null) return BadRequest("Ví không hợp lệ.");
                var projectedBalance = wallet.Balance - oldDelta + newDelta;
                if (projectedBalance < 0)
                    return BadRequest(new { message = "Vượt quá số dư ví." });
                wallet.Balance = projectedBalance;
            }
            else
            {
                var oldWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == dbExpense.WalletId && w.UserId == userId);
                var newWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == request.WalletId && w.UserId == userId);
                if (oldWallet == null || newWallet == null) return BadRequest("Ví không hợp lệ.");

                var projectedOldWalletBalance = oldWallet.Balance - oldDelta;
                var projectedNewWalletBalance = newWallet.Balance + newDelta;
                if (projectedOldWalletBalance < 0 || projectedNewWalletBalance < 0)
                    return BadRequest(new { message = "Vượt quá số dư ví." });

                oldWallet.Balance = projectedOldWalletBalance;
                newWallet.Balance = projectedNewWalletBalance;
            }

            dbExpense.Title = request.Title;
            dbExpense.Amount = normalizedAmount;
            dbExpense.Category = request.Category;
            dbExpense.Date = request.Date == default ? DateTime.Now : request.Date;
            dbExpense.WalletId = request.WalletId;
            dbExpense.Kind = newKind;

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