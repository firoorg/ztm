using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NBitcoin;

namespace Ztm.Data.Entity.Postgres.Migrations
{
    public partial class InitializeTokenReceivingWatcher : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TokenReceivingWatcherRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CallbackId = table.Column<Guid>(nullable: false),
                    PropertyId = table.Column<long>(nullable: false),
                    AddressReservationId = table.Column<Guid>(nullable: false),
                    TargetAmount = table.Column<long>(nullable: false),
                    TargetConfirmation = table.Column<int>(nullable: false),
                    OriginalTimeout = table.Column<TimeSpan>(nullable: false),
                    CurrentTimeout = table.Column<TimeSpan>(nullable: false),
                    TimeoutStatus = table.Column<string>(nullable: false),
                    Status = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenReceivingWatcherRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenReceivingWatcherRules_ReceivingAddressReservations_Add~",
                        column: x => x.AddressReservationId,
                        principalTable: "ReceivingAddressReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TokenReceivingWatcherRules_WebApiCallbacks_CallbackId",
                        column: x => x.CallbackId,
                        principalTable: "WebApiCallbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TokenReceivingWatcherWatches",
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
                    table.PrimaryKey("PK_TokenReceivingWatcherWatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenReceivingWatcherWatches_Blocks_BlockId",
                        column: x => x.BlockId,
                        principalTable: "Blocks",
                        principalColumn: "Hash",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TokenReceivingWatcherWatches_TokenReceivingWatcherRules_Rul~",
                        column: x => x.RuleId,
                        principalTable: "TokenReceivingWatcherRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TokenReceivingWatcherWatches_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Hash",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokenReceivingWatcherRules_AddressReservationId",
                table: "TokenReceivingWatcherRules",
                column: "AddressReservationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenReceivingWatcherRules_CallbackId",
                table: "TokenReceivingWatcherRules",
                column: "CallbackId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenReceivingWatcherRules_PropertyId",
                table: "TokenReceivingWatcherRules",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenReceivingWatcherRules_Status",
                table: "TokenReceivingWatcherRules",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TokenReceivingWatcherWatches_BlockId",
                table: "TokenReceivingWatcherWatches",
                column: "BlockId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenReceivingWatcherWatches_RuleId",
                table: "TokenReceivingWatcherWatches",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenReceivingWatcherWatches_Status",
                table: "TokenReceivingWatcherWatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TokenReceivingWatcherWatches_TransactionId",
                table: "TokenReceivingWatcherWatches",
                column: "TransactionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenReceivingWatcherWatches");

            migrationBuilder.DropTable(
                name: "TokenReceivingWatcherRules");
        }
    }
}
