using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseAndCompletionRequirementVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue: 1 (not 0) for every VersionNo/CourseVersionNo column below — existing
            // rows must backfill to "version 1" to match the VersionNo the C# entity defaults use
            // for anything created from now on (Course.VersionNo, CompletionRequirement.VersionNo
            // both default to 1). Backfilling to 0 would still be internally self-consistent (old
            // ETRs would match old requirement rows at 0==0), but would leave a confusing gap
            // between "1" (code-level meaning of a first version) and "0" (what pre-migration rows
            // would otherwise get) with no functional upside.
            migrationBuilder.AddColumn<int>(
                name: "CourseVersionNo",
                table: "ETRCourseRecords",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "Courses",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<int>(
                name: "VersionNo",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "CompletionRequirements",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "CompletionRequirements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionNo",
                table: "CompletionRequirements",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseVersionNo",
                table: "ETRCourseRecords");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "VersionNo",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "CompletionRequirements");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "CompletionRequirements");

            migrationBuilder.DropColumn(
                name: "VersionNo",
                table: "CompletionRequirements");
        }
    }
}
