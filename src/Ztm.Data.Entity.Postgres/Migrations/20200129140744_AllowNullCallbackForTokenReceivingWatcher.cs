using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ztm.Data.Entity.Postgres.Migrations
{
    public partial class AllowNullCallbackForTokenReceivingWatcher : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TimeoutStatus",
                table: "TokenReceivingWatcherRules",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<Guid>(
                name: "CallbackId",
                table: "TokenReceivingWatcherRules",
                nullable: true,
                oldClrType: typeof(Guid));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TimeoutStatus",
                table: "TokenReceivingWatcherRules",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CallbackId",
                table: "TokenReceivingWatcherRules",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);
        }
    }
}
