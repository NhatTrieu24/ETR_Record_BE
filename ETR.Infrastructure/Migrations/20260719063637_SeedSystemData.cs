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
            migrationBuilder.Sql(@"
                SET QUOTED_IDENTIFIER ON;
                DECLARE @Now DATETIME2 = GETUTCDATE();

                -- Disable all constraints
                EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';

                -- Delete all data except MigrationsHistory
                EXEC sp_MSForEachTable '
                    SET QUOTED_IDENTIFIER ON;
                    IF ''?'' NOT LIKE ''%__EFMigrationsHistory%''
                    BEGIN
                        DELETE FROM ?
                    END
                ';

                -- Reseed all identity columns back to 0 (next inserted will be 1)
                EXEC sp_MSForEachTable '
                    IF ''?'' NOT LIKE ''%__EFMigrationsHistory%'' AND OBJECTPROPERTY(OBJECT_ID(''?''), ''TableHasIdentity'') = 1
                    BEGIN
                        DBCC CHECKIDENT (''?'', RESEED, 0)
                    END
                ';

                -- Re-enable all constraints
                EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';

                -- Seed Roles
                SET IDENTITY_INSERT [Roles] ON;
                INSERT INTO [Roles] (RoleId, RoleName, Description, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 'Admin', 'System Administrator', @Now, 0, NULL),
                (2, 'Instructor', 'Course Instructor', @Now, 0, NULL),
                (3, 'QA', 'Quality Assurance', @Now, 0, NULL),
                (4, 'Academic', 'Academic Staff', @Now, 0, NULL),
                (5, 'TrainingManager', 'Training Manager', @Now, 0, NULL),
                (6, 'Student', 'Student / Learner', @Now, 0, NULL);
                SET IDENTITY_INSERT [Roles] OFF;

                -- Seed Departments (Dummy data just so FKs work)
                SET IDENTITY_INSERT [Departments] ON;
                INSERT INTO [Departments] (DepartmentId, DepartmentName, Description, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 'Administration', 'General Admin', @Now, 0, NULL),
                (2, 'Training', 'Training Dept', @Now, 0, NULL);
                SET IDENTITY_INSERT [Departments] OFF;

                -- Seed Accounts
                SET IDENTITY_INSERT [Accounts] ON;
                INSERT INTO [Accounts] (AccountId, Username, PasswordHash, RoleId, DepartmentId, Status, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 'admin@etr.com', '123456', 1, 1, 'Active', @Now, 0, NULL),
                (2, 'instructor@etr.com', '123456', 2, 2, 'Active', @Now, 0, NULL),
                (3, 'qa@etr.com', '123456', 3, 1, 'Active', @Now, 0, NULL),
                (4, 'academic@etr.com', '123456', 4, 1, 'Active', @Now, 0, NULL),
                (5, 'manager@etr.com', '123456', 5, 2, 'Active', @Now, 0, NULL),
                (6, 'student@etr.com', '123456', 6, 2, 'Active', @Now, 0, NULL);
                SET IDENTITY_INSERT [Accounts] OFF;

                -- Seed UserProfiles
                INSERT INTO [UserProfiles] (AccountId, UserCode, FullName, Email, DateOfBirth, Gender, CreatedAt, IsDeleted, CreatedByAccountId) VALUES 
                (1, 'ADM-01', 'System Admin', 'admin@etr.com', '1980-01-01', 'Other', @Now, 0, 1),
                (2, 'INS-01', 'Senior Instructor', 'instructor@etr.com', '1985-01-01', 'Male', @Now, 0, 1),
                (3, 'QA-01', 'QA Specialist', 'qa@etr.com', '1990-01-01', 'Female', @Now, 0, 1),
                (4, 'ACA-01', 'Academic Staff', 'academic@etr.com', '1992-01-01', 'Female', @Now, 0, 1),
                (5, 'MGR-01', 'Training Manager', 'manager@etr.com', '1988-01-01', 'Male', @Now, 0, 1),
                (6, 'STU-01', 'Jane Student', 'student@etr.com', '2000-01-01', 'Female', @Now, 0, 1);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
