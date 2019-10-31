using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NBitcoin;

namespace Ztm.Data.Entity.Postgres.Migrations
{
    public partial class InitializeTransactionConfirmationWatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransactionConfirmationWatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CallbackId = table.Column<Guid>(nullable: false),
                    Transaction = table.Column<uint256>(nullable: false),
                    Confirmation = table.Column<int>(nullable: false),
                    Due = table.Column<DateTime>(nullable: false),
                    SuccessData = table.Column<string>(type: "jsonb", nullable: false),
                    TimeoutData = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionConfirmationWatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionConfirmationWatches_WebApiCallbacks_CallbackId",
                        column: x => x.CallbackId,
                        principalTable: "WebApiCallbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionConfirmationWatches_CallbackId",
                table: "TransactionConfirmationWatches",
                column: "CallbackId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransactionConfirmationWatches");
        }
    }
}
