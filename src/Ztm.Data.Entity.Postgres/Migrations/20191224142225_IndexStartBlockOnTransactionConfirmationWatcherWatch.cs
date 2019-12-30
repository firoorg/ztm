using Microsoft.EntityFrameworkCore.Migrations;

namespace Ztm.Data.Entity.Postgres.Migrations
{
    public partial class IndexStartBlockOnTransactionConfirmationWatcherWatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                table: "TransactionConfirmationWatcherRules");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionConfirmationWatcherWatches_StartBlockHash",
                table: "TransactionConfirmationWatcherWatches",
                column: "StartBlockHash");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransactionConfirmationWatcherWatches_StartBlockHash",
                table: "TransactionConfirmationWatcherWatches");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "TransactionConfirmationWatcherRules",
                nullable: true);
        }
    }
}
