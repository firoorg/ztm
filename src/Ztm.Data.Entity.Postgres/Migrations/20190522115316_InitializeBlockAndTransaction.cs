using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ztm.Data.Entity.Postgres.Migrations
{
    public partial class InitializeBlockAndTransaction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "webapi_callback_id_pkey",
                table: "webapi_callback");

            migrationBuilder.RenameTable(
                name: "webapi_callback",
                newName: "WebApiCallbacks");

            migrationBuilder.RenameColumn(
                name: "url",
                table: "WebApiCallbacks",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "WebApiCallbacks",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "transaction_id",
                table: "WebApiCallbacks",
                newName: "TransactionId");

            migrationBuilder.RenameColumn(
                name: "request_time",
                table: "WebApiCallbacks",
                newName: "RequestTime");

            migrationBuilder.RenameColumn(
                name: "request_ip",
                table: "WebApiCallbacks",
                newName: "RequestIp");

            migrationBuilder.RenameIndex(
                name: "webapi_callback_transaction_id_idx",
                table: "WebApiCallbacks",
                newName: "IX_WebApiCallbacks_TransactionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WebApiCallbacks",
                table: "WebApiCallbacks",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Blocks",
                columns: table => new
                {
                    Height = table.Column<int>(nullable: false),
                    Hash = table.Column<byte[]>(nullable: false),
                    Version = table.Column<int>(nullable: false),
                    Bits = table.Column<long>(nullable: false),
                    Nonce = table.Column<long>(nullable: false),
                    Time = table.Column<DateTime>(nullable: false),
                    MerkleRoot = table.Column<byte[]>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blocks", x => x.Height);
                    table.UniqueConstraint("AK_Blocks_Hash", x => x.Hash);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Hash = table.Column<byte[]>(nullable: false),
                    Version = table.Column<long>(nullable: false),
                    LockTime = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Hash);
                });

            migrationBuilder.CreateTable(
                name: "BlockTransactions",
                columns: table => new
                {
                    BlockHash = table.Column<byte[]>(nullable: false),
                    TransactionHash = table.Column<byte[]>(nullable: false),
                    Index = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockTransactions", x => new { x.BlockHash, x.TransactionHash, x.Index });
                    table.ForeignKey(
                        name: "FK_BlockTransactions_Blocks_BlockHash",
                        column: x => x.BlockHash,
                        principalTable: "Blocks",
                        principalColumn: "Hash",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlockTransactions_Transactions_TransactionHash",
                        column: x => x.TransactionHash,
                        principalTable: "Transactions",
                        principalColumn: "Hash",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Outputs",
                columns: table => new
                {
                    TransactionHash = table.Column<byte[]>(nullable: false),
                    Index = table.Column<long>(nullable: false),
                    Value = table.Column<long>(nullable: false),
                    Script = table.Column<byte[]>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outputs", x => new { x.TransactionHash, x.Index });
                    table.ForeignKey(
                        name: "FK_Outputs_Transactions_TransactionHash",
                        column: x => x.TransactionHash,
                        principalTable: "Transactions",
                        principalColumn: "Hash",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Inputs",
                columns: table => new
                {
                    TransactionHash = table.Column<byte[]>(nullable: false),
                    Index = table.Column<long>(nullable: false),
                    OutputHash = table.Column<byte[]>(nullable: false),
                    OutputIndex = table.Column<long>(nullable: false),
                    Script = table.Column<byte[]>(nullable: false),
                    Sequence = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inputs", x => new { x.TransactionHash, x.Index });
                    table.ForeignKey(
                        name: "FK_Inputs_Transactions_TransactionHash",
                        column: x => x.TransactionHash,
                        principalTable: "Transactions",
                        principalColumn: "Hash",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Inputs_Outputs_OutputHash_OutputIndex",
                        columns: x => new { x.OutputHash, x.OutputIndex },
                        principalTable: "Outputs",
                        principalColumns: new[] { "TransactionHash", "Index" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlockTransactions_TransactionHash",
                table: "BlockTransactions",
                column: "TransactionHash");

            migrationBuilder.CreateIndex(
                name: "IX_Inputs_OutputHash_OutputIndex",
                table: "Inputs",
                columns: new[] { "OutputHash", "OutputIndex" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockTransactions");

            migrationBuilder.DropTable(
                name: "Inputs");

            migrationBuilder.DropTable(
                name: "Blocks");

            migrationBuilder.DropTable(
                name: "Outputs");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WebApiCallbacks",
                table: "WebApiCallbacks");

            migrationBuilder.RenameTable(
                name: "WebApiCallbacks",
                newName: "webapi_callback");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "webapi_callback",
                newName: "url");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "webapi_callback",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TransactionId",
                table: "webapi_callback",
                newName: "transaction_id");

            migrationBuilder.RenameColumn(
                name: "RequestTime",
                table: "webapi_callback",
                newName: "request_time");

            migrationBuilder.RenameColumn(
                name: "RequestIp",
                table: "webapi_callback",
                newName: "request_ip");

            migrationBuilder.RenameIndex(
                name: "IX_WebApiCallbacks_TransactionId",
                table: "webapi_callback",
                newName: "webapi_callback_transaction_id_idx");

            migrationBuilder.AddPrimaryKey(
                name: "webapi_callback_id_pkey",
                table: "webapi_callback",
                column: "id");
        }
    }
}
