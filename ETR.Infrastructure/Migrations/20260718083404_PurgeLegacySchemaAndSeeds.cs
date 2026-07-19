using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PurgeLegacySchemaAndSeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_LearnerTypes_LearnerTypeId",
                table: "UserProfiles");

            migrationBuilder.DropTable(
                name: "LearnerTypes");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_LearnerTypeId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "LearnerTypeId",
                table: "UserProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LearnerTypeId",
                table: "UserProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LearnerTypes",
                columns: table => new
                {
                    LearnerTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByAccountId = table.Column<int>(type: "int", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TypeName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByAccountId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerTypes", x => x.LearnerTypeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_LearnerTypeId",
                table: "UserProfiles",
                column: "LearnerTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerTypes_TypeName",
                table: "LearnerTypes",
                column: "TypeName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_LearnerTypes_LearnerTypeId",
                table: "UserProfiles",
                column: "LearnerTypeId",
                principalTable: "LearnerTypes",
                principalColumn: "LearnerTypeId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
