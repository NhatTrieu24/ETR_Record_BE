using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedSystemData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Originally seeded Roles/Departments/Accounts/UserProfiles here via raw SQL. Removed because:
            // (1) it used sp_MSForEachTable, an internal proc that only exists on boxed/on-prem SQL Server
            //     (including our local Docker container) — Azure SQL Database doesn't have it at all, so
            //     this migration failed outright ("Could not find stored procedure") on every fresh Azure DB;
            // (2) it inserted plaintext PasswordHash values ("123456"), which silently satisfied
            //     DataSeeder.SeedIdentityAsync's "if (!AnyAsync())" guard and left accounts whose hash could
            //     never verify via BCrypt — logins broke on any brand-new database.
            // This migration runs immediately after InitialCreate, so every table it used to wipe is always
            // empty at this point in the chain — the wipe/reseed-identity logic was already a no-op in every
            // real execution. DataSeeder.cs (invoked automatically after MigrateAsync in Program.cs) is now
            // the sole source of truth for this baseline data, with a proper bcrypt hash per account.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
