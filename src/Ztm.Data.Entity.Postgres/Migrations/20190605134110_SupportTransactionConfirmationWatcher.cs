using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ztm.Data.Entity.Postgres.Migrations
{
    public partial class SupportTransactionConfirmationWatcher : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WatchingTransactions",
                columns: table => new
                {
                    Hash = table.Column<byte[]>(nullable: false),
                    Listener = table.Column<Guid>(nullable: false),
                    StartTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchingTransactions", x => new { x.Hash, x.Listener });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WatchingTransactions");
        }
    }
}
