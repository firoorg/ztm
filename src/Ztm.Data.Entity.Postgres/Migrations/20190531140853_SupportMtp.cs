using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ztm.Data.Entity.Postgres.Migrations
{
    public partial class SupportMtp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "MtpHashValue",
                table: "Blocks",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MtpVersion",
                table: "Blocks",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Reserved1",
                table: "Blocks",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Reserved2",
                table: "Blocks",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MtpHashValue",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "MtpVersion",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "Reserved1",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "Reserved2",
                table: "Blocks");
        }
    }
}
