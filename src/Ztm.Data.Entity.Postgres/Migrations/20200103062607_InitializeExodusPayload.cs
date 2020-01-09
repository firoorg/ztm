using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NBitcoin;

namespace Ztm.Data.Entity.Postgres.Migrations
{
    public partial class InitializeExodusPayload : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExodusPayloads",
                columns: table => new
                {
                    TransactionHash = table.Column<uint256>(nullable: false),
                    Sender = table.Column<string>(nullable: false),
                    Receiver = table.Column<string>(nullable: false),
                    Data = table.Column<byte[]>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExodusPayloads", x => x.TransactionHash);
                    table.ForeignKey(
                        name: "FK_ExodusPayloads_Transactions_TransactionHash",
                        column: x => x.TransactionHash,
                        principalTable: "Transactions",
                        principalColumn: "Hash",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExodusPayloads");
        }
    }
}
