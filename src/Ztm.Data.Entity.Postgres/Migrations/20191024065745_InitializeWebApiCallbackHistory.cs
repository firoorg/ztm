using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NBitcoin;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Ztm.Data.Entity.Postgres.Migrations
{
    public partial class InitializeWebApiCallbackHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WebApiCallbacks_TransactionId",
                table: "WebApiCallbacks");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "WebApiCallbacks");

            migrationBuilder.RenameColumn(
                name: "RequestTime",
                table: "WebApiCallbacks",
                newName: "RegisteredTime");

            migrationBuilder.RenameColumn(
                name: "RequestIp",
                table: "WebApiCallbacks",
                newName: "RegisteredIp");

            migrationBuilder.AddColumn<bool>(
                name: "Completed",
                table: "WebApiCallbacks",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "WebApiCallbackHistories",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    CallbackId = table.Column<Guid>(nullable: false),
                    Status = table.Column<string>(nullable: false),
                    InvokedTime = table.Column<DateTime>(nullable: false),
                    Data = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebApiCallbackHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebApiCallbackHistories_WebApiCallbacks_CallbackId",
                        column: x => x.CallbackId,
                        principalTable: "WebApiCallbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebApiCallbackHistories_CallbackId",
                table: "WebApiCallbackHistories",
                column: "CallbackId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebApiCallbackHistories");

            migrationBuilder.DropColumn(
                name: "Completed",
                table: "WebApiCallbacks");

            migrationBuilder.RenameColumn(
                name: "RegisteredTime",
                table: "WebApiCallbacks",
                newName: "RequestTime");

            migrationBuilder.RenameColumn(
                name: "RegisteredIp",
                table: "WebApiCallbacks",
                newName: "RequestIp");

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
