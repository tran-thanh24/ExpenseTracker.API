namespace ExpenseTracker.API.Models
{
    public static class TransactionKindExtensions
    {
        public static decimal ToWalletDelta(this TransactionKind kind, decimal amount) =>
            kind == TransactionKind.Income ? amount : -amount;
    }
}
