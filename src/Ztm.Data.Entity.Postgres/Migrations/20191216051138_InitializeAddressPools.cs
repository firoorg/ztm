using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ztm.Data.Entity.Postgres.Migrations
{
    public partial class InitializeAddressPools : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceivingAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Address = table.Column<string>(nullable: false),
                    IsLocked = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivingAddresses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReceivingAddressReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    AddressId = table.Column<Guid>(nullable: false),
                    LockedAt = table.Column<DateTime>(nullable: false),
                    ReleasedAt = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivingAddressReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceivingAddressReservations_ReceivingAddresses_AddressId",
                        column: x => x.AddressId,
                        principalTable: "ReceivingAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivingAddresses_Address",
                table: "ReceivingAddresses",
                column: "Address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceivingAddressReservations_AddressId",
                table: "ReceivingAddressReservations",
                column: "AddressId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceivingAddressReservations");

            migrationBuilder.DropTable(
                name: "ReceivingAddresses");
        }
    }
}
