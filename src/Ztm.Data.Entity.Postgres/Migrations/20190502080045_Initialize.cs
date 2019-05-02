using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ztm.Data.Entity.Postgres.Migrations
{
    public partial class Initialize : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "webapi_callback",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    request_ip = table.Column<IPAddress>(nullable: false),
                    request_time = table.Column<DateTime>(nullable: false),
                    transaction_id = table.Column<byte[]>(nullable: true),
                    url = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("webapi_callback_id_pkey", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "webapi_callback_transaction_id_idx",
                table: "webapi_callback",
                column: "transaction_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "webapi_callback");
        }
    }
}
