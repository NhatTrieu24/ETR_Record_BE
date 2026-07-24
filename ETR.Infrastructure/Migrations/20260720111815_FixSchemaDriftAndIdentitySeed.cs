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
            // sp_MSForEachTable is an on-prem/boxed-SQL-Server-only internal proc — unavailable on
            // Azure SQL Database. Portable equivalent via sys.tables + sp_executesql (same pattern
            // already used in Deploy_NukeAndSeed.sql) so this migration works on any SQL Server target.
            migrationBuilder.Sql(@"
                DECLARE @reseedSql NVARCHAR(MAX) = N'';
                SELECT @reseedSql += N'
                    IF NOT EXISTS (SELECT 1 FROM ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N')
                    BEGIN
                        DBCC CHECKIDENT (''' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N''', RESEED, 1);
                    END'
                FROM sys.tables t
                JOIN sys.schemas s ON t.schema_id = s.schema_id
                WHERE t.name <> '__EFMigrationsHistory' AND OBJECTPROPERTY(t.object_id, 'TableHasIdentity') = 1;

                EXEC sp_executesql @reseedSql;
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
