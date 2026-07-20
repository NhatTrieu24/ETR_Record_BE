using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSchemaDriftAndIdentitySeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AssessmentResults never got the SessionId column that
            // AddSessionIdToPracticalChecklistResult added to PracticalChecklistResults —
            // the model/snapshot already expects it, so bring the physical table in line.
            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                table: "AssessmentResults",
                type: "int",
                nullable: true);

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResults_AssessmentId_AccountId",
                table: "AssessmentResults");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_AssessmentId_AccountId_SessionId",
                table: "AssessmentResults",
                columns: new[] { "AssessmentId", "AccountId", "SessionId" },
                unique: true,
                filter: "[SessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_SessionId",
                table: "AssessmentResults",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentResults_Sessions_SessionId",
                table: "AssessmentResults",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.Restrict);

            // PracticalChecklistResults was refactored in code (IsCompleted -> Score +
            // ResultStatus, plus IsPublished/PublishedAt) but no migration ever applied
            // that change to the physical table. Bring it in line too.
            migrationBuilder.AddColumn<decimal>(
                name: "Score",
                table: "PracticalChecklistResults",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ResultStatus",
                table: "PracticalChecklistResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "PracticalChecklistResults",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "PracticalChecklistResults",
                type: "datetime2",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "PracticalChecklistResults");

            // SeedSystemData's blanket "DBCC CHECKIDENT (?, RESEED, 0)" over every table
            // means the first row ever inserted into any table it didn't itself populate
            // receives identity value 0 (SQL Server's documented empty-table RESEED rule) —
            // indistinguishable from an unset key/FK to EF Core. Reseed every currently-empty
            // identity table so its next row starts at 1 instead.
            migrationBuilder.Sql(@"
                EXEC sp_MSForEachTable '
                    SET QUOTED_IDENTIFIER ON;
                    IF ''?'' NOT LIKE ''%__EFMigrationsHistory%''
                       AND OBJECTPROPERTY(OBJECT_ID(''?''), ''TableHasIdentity'') = 1
                       AND NOT EXISTS (SELECT 1 FROM ?)
                    BEGIN
                        DBCC CHECKIDENT (''?'', RESEED, 1)
                    END
                ';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "PracticalChecklistResults",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "PracticalChecklistResults");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "PracticalChecklistResults");

            migrationBuilder.DropColumn(
                name: "ResultStatus",
                table: "PracticalChecklistResults");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "PracticalChecklistResults");

            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentResults_Sessions_SessionId",
                table: "AssessmentResults");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResults_SessionId",
                table: "AssessmentResults");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResults_AssessmentId_AccountId_SessionId",
                table: "AssessmentResults");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_AssessmentId_AccountId",
                table: "AssessmentResults",
                columns: new[] { "AssessmentId", "AccountId" },
                unique: true);

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "AssessmentResults");

            // Identity reseed is not reversible (we don't know the prior seed value).
        }
    }
}
