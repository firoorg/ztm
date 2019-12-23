using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NBitcoin;

namespace Ztm.Data.Entity.Postgres.Migrations
{
    public partial class InitializeTransactionConfirmationWatchers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransactionConfirmationWatcherWatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    RuleId = table.Column<Guid>(nullable: false),
                    StartBlockHash = table.Column<uint256>(nullable: false),
                    StartTime = table.Column<DateTime>(nullable: false),
                    TransactionHash = table.Column<uint256>(nullable: false),
                    Status = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionConfirmationWatcherWatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransactionConfirmationWatcherRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CallbackId = table.Column<Guid>(nullable: false),
                    TransactionHash = table.Column<uint256>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    Confirmation = table.Column<int>(nullable: false),
                    OriginalWaitingTime = table.Column<TimeSpan>(nullable: false),
                    RemainingWaitingTime = table.Column<TimeSpan>(nullable: false),
                    SuccessData = table.Column<string>(type: "jsonb", nullable: false),
                    TimeoutData = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    CurrentWatchId = table.Column<Guid>(nullable: true),
                    Note = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionConfirmationWatcherRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionConfirmationWatcherRules_WebApiCallbacks_Callbac~",
                        column: x => x.CallbackId,
                        principalTable: "WebApiCallbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionConfirmationWatcherRules_TransactionConfirmation~",
                        column: x => x.CurrentWatchId,
                        principalTable: "TransactionConfirmationWatcherWatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionConfirmationWatcherRules_CallbackId",
                table: "TransactionConfirmationWatcherRules",
                column: "CallbackId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionConfirmationWatcherRules_CurrentWatchId",
                table: "TransactionConfirmationWatcherRules",
                column: "CurrentWatchId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionConfirmationWatcherRules_Status",
                table: "TransactionConfirmationWatcherRules",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionConfirmationWatcherWatches_RuleId",
                table: "TransactionConfirmationWatcherWatches",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionConfirmationWatcherWatches_Status",
                table: "TransactionConfirmationWatcherWatches",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionConfirmationWatcherWatches_TransactionConfirmati~",
                table: "TransactionConfirmationWatcherWatches",
                column: "RuleId",
                principalTable: "TransactionConfirmationWatcherRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionConfirmationWatcherRules_TransactionConfirmation~",
                table: "TransactionConfirmationWatcherRules");

            migrationBuilder.DropTable(
                name: "TransactionConfirmationWatcherWatches");

            migrationBuilder.DropTable(
                name: "TransactionConfirmationWatcherRules");
        }
    }
}
