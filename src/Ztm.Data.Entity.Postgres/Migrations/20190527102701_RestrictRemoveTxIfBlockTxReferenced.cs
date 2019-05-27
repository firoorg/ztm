using Microsoft.EntityFrameworkCore.Migrations;

namespace Ztm.Data.Entity.Postgres.Migrations
{
    public partial class RestrictRemoveTxIfBlockTxReferenced : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlockTransactions_Transactions_TransactionHash",
                table: "BlockTransactions");

            migrationBuilder.AddForeignKey(
                name: "FK_BlockTransactions_Transactions_TransactionHash",
                table: "BlockTransactions",
                column: "TransactionHash",
                principalTable: "Transactions",
                principalColumn: "Hash",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlockTransactions_Transactions_TransactionHash",
                table: "BlockTransactions");

            migrationBuilder.AddForeignKey(
                name: "FK_BlockTransactions_Transactions_TransactionHash",
                table: "BlockTransactions",
                column: "TransactionHash",
                principalTable: "Transactions",
                principalColumn: "Hash",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
