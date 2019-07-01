using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ztm.Data.Entity.Postgres.Migrations
{
    public partial class RemoveWatchingAddresses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WatchingAddresses");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WatchingAddresses",
                columns: table => new
                {
                    Address = table.Column<string>(maxLength: 64, nullable: false),
                    Type = table.Column<byte>(nullable: false),
                    Listener = table.Column<Guid>(nullable: false),
                    StartTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchingAddresses", x => new { x.Address, x.Type, x.Listener });
                });
        }
    }
}
