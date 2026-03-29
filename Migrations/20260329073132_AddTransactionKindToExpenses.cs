using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionKindToExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Expenses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Dữ liệu cũ: Amount âm = Chi, dương = Thu — chuẩn hóa Amount dương + Kind.
            migrationBuilder.Sql("""
                UPDATE [Expenses]
                SET [Kind] = CASE WHEN [Amount] < 0 THEN 0 ELSE 1 END;

                UPDATE [Expenses]
                SET [Amount] = ABS([Amount]);

                UPDATE [w]
                SET [Balance] = COALESCE([agg].[SumDelta], 0)
                FROM [Wallets] AS [w]
                LEFT JOIN (
                    SELECT [WalletId],
                           SUM(CASE WHEN [Kind] = 1 THEN [Amount] ELSE -[Amount] END) AS [SumDelta]
                    FROM [Expenses]
                    GROUP BY [WalletId]
                ) AS [agg] ON [agg].[WalletId] = [w].[Id];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Expenses");
        }
    }
}
