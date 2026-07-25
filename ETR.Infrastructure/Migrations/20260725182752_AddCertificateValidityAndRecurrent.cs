using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateValidityAndRecurrent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "ETRCourseRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedDate",
                table: "ETRCourseRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreviousRecordId",
                table: "ETRCourseRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CourseType",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValidityMonths",
                table: "Courses",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "ETRCourseRecords");

            migrationBuilder.DropColumn(
                name: "IssuedDate",
                table: "ETRCourseRecords");

            migrationBuilder.DropColumn(
                name: "PreviousRecordId",
                table: "ETRCourseRecords");

            migrationBuilder.DropColumn(
                name: "CourseType",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "ValidityMonths",
                table: "Courses");
        }
    }
}
