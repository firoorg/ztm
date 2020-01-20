using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NBitcoin;

namespace Ztm.Data.Entity.Postgres.Migrations
{
    public partial class InitializeTokenBalanceWatcher : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TokenBalanceWatcherRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CallbackId = table.Column<Guid>(nullable: false),
                    PropertyId = table.Column<long>(nullable: false),
                    Address = table.Column<string>(nullable: false),
                    TargetAmount = table.Column<long>(nullable: false),
                    TargetConfirmation = table.Column<int>(nullable: false),
                    OriginalTimeout = table.Column<TimeSpan>(nullable: false),
                    CurrentTimeout = table.Column<TimeSpan>(nullable: false),
                    TimeoutStatus = table.Column<string>(nullable: false),
                    Status = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenBalanceWatcherRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenBalanceWatcherRules_WebApiCallbacks_CallbackId",
                        column: x => x.CallbackId,
                        principalTable: "WebApiCallbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TokenBalanceWatcherWatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    RuleId = table.Column<Guid>(nullable: false),
                    BlockId = table.Column<uint256>(nullable: false),
                    TransactionId = table.Column<uint256>(nullable: false),
                    BalanceChange = table.Column<long>(nullable: false),
                    CreatedTime = table.Column<DateTime>(nullable: false),
                    Confirmation = table.Column<int>(nullable: false),
                    Status = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenBalanceWatcherWatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenBalanceWatcherWatches_TokenBalanceWatcherRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "TokenBalanceWatcherRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalanceWatcherRules_CallbackId",
                table: "TokenBalanceWatcherRules",
                column: "CallbackId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalanceWatcherRules_PropertyId",
                table: "TokenBalanceWatcherRules",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalanceWatcherRules_Status",
                table: "TokenBalanceWatcherRules",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalanceWatcherWatches_BlockId",
                table: "TokenBalanceWatcherWatches",
                column: "BlockId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalanceWatcherWatches_RuleId",
                table: "TokenBalanceWatcherWatches",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenBalanceWatcherWatches_Status",
                table: "TokenBalanceWatcherWatches",
                column: "Status");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenBalanceWatcherWatches");

            migrationBuilder.DropTable(
                name: "TokenBalanceWatcherRules");
        }
    }
}
