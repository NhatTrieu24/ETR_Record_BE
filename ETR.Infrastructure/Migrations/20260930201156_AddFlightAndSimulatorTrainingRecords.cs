using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightAndSimulatorTrainingRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LessonCode",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrainingType",
                table: "Sessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Theory");

            migrationBuilder.AddColumn<string>(
                name: "AircraftRegistration",
                table: "AttendanceRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArrivalIcao",
                table: "AttendanceRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CrossCountryHours",
                table: "AttendanceRecords",
                type: "decimal(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DayLandings",
                table: "AttendanceRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepartureIcao",
                table: "AttendanceRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DualHours",
                table: "AttendanceRecords",
                type: "decimal(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FlightHours",
                table: "AttendanceRecords",
                type: "decimal(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstructorComments",
                table: "AttendanceRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InstructorSignedAt",
                table: "AttendanceRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstructorSignedByAccountId",
                table: "AttendanceRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InstrumentHours",
                table: "AttendanceRecords",
                type: "decimal(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NightHours",
                table: "AttendanceRecords",
                type: "decimal(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NightLandings",
                table: "AttendanceRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PerformanceGrade",
                table: "AttendanceRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PicHours",
                table: "AttendanceRecords",
                type: "decimal(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Route",
                table: "AttendanceRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SimulatorDevice",
                table: "AttendanceRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SimulatorHours",
                table: "AttendanceRecords",
                type: "decimal(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SoloHours",
                table: "AttendanceRecords",
                type: "decimal(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentComments",
                table: "AttendanceRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StudentSignedAt",
                table: "AttendanceRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudentSignedByAccountId",
                table: "AttendanceRecords",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LessonCode",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "TrainingType",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "AircraftRegistration",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "ArrivalIcao",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "CrossCountryHours",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "DayLandings",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "DepartureIcao",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "DualHours",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "FlightHours",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "InstructorComments",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "InstructorSignedAt",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "InstructorSignedByAccountId",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "InstrumentHours",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "NightHours",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "NightLandings",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "PerformanceGrade",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "PicHours",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "Route",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "SimulatorDevice",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "SimulatorHours",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "SoloHours",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "StudentComments",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "StudentSignedAt",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "StudentSignedByAccountId",
                table: "AttendanceRecords");
        }
    }
}
