using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstructorAccountIdToClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InstructorAccountId",
                table: "Classes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_InstructorAccountId",
                table: "Classes",
                column: "InstructorAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Accounts_InstructorAccountId",
                table: "Classes",
                column: "InstructorAccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Accounts_InstructorAccountId",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_Classes_InstructorAccountId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "InstructorAccountId",
                table: "Classes");
        }
    }
}
