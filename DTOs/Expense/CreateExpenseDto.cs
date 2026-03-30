using System;
using ExpenseTracker.API.Models;

namespace ExpenseTracker.API.DTOs
{
    public class ExpenseUpsertDto
    {
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Category { get; set; } = "General";
        public DateTime Date { get; set; }
        public int WalletId { get; set; }
        public TransactionKind? Kind { get; set; }
    }
}