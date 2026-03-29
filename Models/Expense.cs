using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExpenseTracker.API.Models
{
    public class Expense
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Category { get; set; } = "General";
        public DateTime Date { get; set; } = DateTime.UtcNow;

        public TransactionKind Kind { get; set; } = TransactionKind.Expense;

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public Users? User { get; set; }

        public int WalletId { get; set; }
    }
}
