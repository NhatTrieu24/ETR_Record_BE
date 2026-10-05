using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingFacilityAndSessionTimeSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingFacilities",
                columns: table => new
                {
                    FacilityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FacilityCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FacilityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FacilityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocationDetail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByAccountId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByAccountId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByAccountId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingFacilities", x => x.FacilityId);
                });

            migrationBuilder.AddColumn<int>(
                name: "DefaultFacilityId",
                table: "Classes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FacilityId",
                table: "Sessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartAt",
                table: "Sessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndAt",
                table: "Sessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRemedial",
                table: "Sessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingFacilities_FacilityCode",
                table: "TrainingFacilities",
                column: "FacilityCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_DefaultFacilityId",
                table: "Classes",
                column: "DefaultFacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_FacilityId",
                table: "Sessions",
                column: "FacilityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_TrainingFacilities_DefaultFacilityId",
                table: "Classes",
                column: "DefaultFacilityId",
                principalTable: "TrainingFacilities",
                principalColumn: "FacilityId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_TrainingFacilities_FacilityId",
                table: "Sessions",
                column: "FacilityId",
                principalTable: "TrainingFacilities",
                principalColumn: "FacilityId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_TrainingFacilities_DefaultFacilityId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_TrainingFacilities_FacilityId",
                table: "Sessions");

            migrationBuilder.DropTable(
                name: "TrainingFacilities");

            migrationBuilder.DropIndex(
                name: "IX_Classes_DefaultFacilityId",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_FacilityId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "DefaultFacilityId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "FacilityId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "StartAt",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "EndAt",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "IsRemedial",
                table: "Sessions");
        }
    }
}
