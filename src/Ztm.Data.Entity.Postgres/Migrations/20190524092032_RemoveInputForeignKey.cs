using Microsoft.EntityFrameworkCore.Migrations;

namespace Ztm.Data.Entity.Postgres.Migrations
{
    public partial class RemoveInputForeignKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inputs_Outputs_OutputHash_OutputIndex",
                table: "Inputs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_Inputs_Outputs_OutputHash_OutputIndex",
                table: "Inputs",
                columns: new[] { "OutputHash", "OutputIndex" },
                principalTable: "Outputs",
                principalColumns: new[] { "TransactionHash", "Index" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
