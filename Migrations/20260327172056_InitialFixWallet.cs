using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialFixWallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Wallets_Users_UsersId",
                table: "Wallets");

            migrationBuilder.DropIndex(
                name: "IX_Wallets_UsersId",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "UsersId",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "WalletId",
                table: "Expenses");

            migrationBuilder.RenameColumn(
                name: "Note",
                table: "Expenses",
                newName: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_UserId",
                table: "Expenses",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Users_UserId",
                table: "Expenses",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Users_UserId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_UserId",
                table: "Expenses");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Expenses",
                newName: "Note");

            migrationBuilder.AddColumn<int>(
                name: "UsersId",
                table: "Wallets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WalletId",
                table: "Expenses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UsersId",
                table: "Wallets",
                column: "UsersId");

            migrationBuilder.AddForeignKey(
                name: "FK_Wallets_Users_UsersId",
                table: "Wallets",
                column: "UsersId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
