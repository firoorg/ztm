using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NBitcoin;

namespace Ztm.Data.Entity.Postgres.Migrations
{
    public partial class InitializeCallbackInvocation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WebApiCallbacks_TransactionId",
                table: "WebApiCallbacks");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "WebApiCallbacks");

            migrationBuilder.AddColumn<bool>(
                name: "Completed",
                table: "WebApiCallbacks",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CallbackInvocations",
                columns: table => new
                {
                    CallbackId = table.Column<Guid>(nullable: false),
                    InvokedTime = table.Column<DateTime>(nullable: false),
                    Status = table.Column<string>(nullable: false),
                    Data = table.Column<byte[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallbackInvocations", x => new { x.CallbackId, x.InvokedTime });
                    table.ForeignKey(
                        name: "FK_CallbackInvocations_WebApiCallbacks_CallbackId",
                        column: x => x.CallbackId,
                        principalTable: "WebApiCallbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CallbackInvocations");

            migrationBuilder.DropColumn(
                name: "Completed",
                table: "WebApiCallbacks");

            migrationBuilder.AddColumn<uint256>(
                name: "TransactionId",
                table: "WebApiCallbacks",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebApiCallbacks_TransactionId",
                table: "WebApiCallbacks",
                column: "TransactionId");
        }
    }
}
