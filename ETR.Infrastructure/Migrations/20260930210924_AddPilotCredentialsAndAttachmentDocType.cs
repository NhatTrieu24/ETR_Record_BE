using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPilotCredentialsAndAttachmentDocType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CredentialsVerifiedAt",
                table: "UserProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CredentialsVerifiedByAccountId",
                table: "UserProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IcaoElpExpiryDate",
                table: "UserProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IcaoElpLevel",
                table: "UserProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCredentialsVerified",
                table: "UserProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LicenseExpiryDate",
                table: "UserProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenseNumber",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenseType",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalClass",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MedicalExpiryDate",
                table: "UserProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypeRatings",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocType",
                table: "Attachments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CredentialsVerifiedAt",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "CredentialsVerifiedByAccountId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "IcaoElpExpiryDate",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "IcaoElpLevel",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "IsCredentialsVerified",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "LicenseExpiryDate",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "LicenseNumber",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "LicenseType",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "MedicalClass",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "MedicalExpiryDate",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TypeRatings",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "DocType",
                table: "Attachments");
        }
    }
}
