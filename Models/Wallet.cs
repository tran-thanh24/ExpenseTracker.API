namespace ExpenseTracker.API.Models
{
    public class Wallet
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public int UserId { get; set; }
        public Users Users { get; set; } = null!;
    }
}