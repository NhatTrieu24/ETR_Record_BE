using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseDepartmentsAndTrainingAudienceFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add DepartmentCode and IsTrainingAudience columns
            migrationBuilder.AddColumn<string>(
                name: "DepartmentCode",
                table: "Departments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsTrainingAudience",
                table: "Departments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // 2. Data cleansing, seed, backfill, and collision resolution (executed BEFORE creating unique index)
            migrationBuilder.Sql(@"
                -- 2.1 Backfill empty or whitespace codes with unique DEPT-{DepartmentId}
                UPDATE [Departments]
                SET [DepartmentCode] = CONCAT(N'DEPT-', [DepartmentId])
                WHERE ([DepartmentCode] IS NULL OR [DepartmentCode] = N'' OR LTRIM(RTRIM([DepartmentCode])) = N'')
                  AND [IsDeleted] = 0;

                -- 2.2 Resolve collisions: Release standard codes if held by non-standard custom departments
                UPDATE [Departments]
                SET [DepartmentCode] = CONCAT(N'DEPT-', [DepartmentId])
                WHERE [DepartmentCode] IN (N'ADM', N'TRN', N'FC', N'CC', N'ENG', N'GND')
                  AND [DepartmentName] NOT IN (N'Administration', N'Training', N'Flight Crew', N'Cabin Crew', N'Engineering & Maintenance', N'Ground Operations')
                  AND [IsDeleted] = 0;

                -- 2.3 Upsert 6 standard departments (ensuring exactly one active row receives each standard code)
                -- Administration (Internal Admin)
                IF EXISTS (SELECT 1 FROM [Departments] WHERE [DepartmentName] = N'Administration' AND [IsDeleted] = 0)
                    UPDATE [Departments] SET [DepartmentCode] = N'ADM', [IsTrainingAudience] = 0, [Description] = COALESCE(NULLIF([Description], N''), N'Ban giám hiệu & Quản trị hệ thống')
                    WHERE [DepartmentId] = (SELECT MIN([DepartmentId]) FROM [Departments] WHERE [DepartmentName] = N'Administration' AND [IsDeleted] = 0);
                ELSE IF NOT EXISTS (SELECT 1 FROM [Departments] WHERE [DepartmentCode] = N'ADM' AND [IsDeleted] = 0)
                    INSERT INTO [Departments] ([DepartmentName], [DepartmentCode], [Description], [IsTrainingAudience], [CreatedAt], [IsDeleted])
                    VALUES (N'Administration', N'ADM', N'Ban giám hiệu & Quản trị hệ thống', 0, GETUTCDATE(), 0);

                -- Training (Internal Academic/Training)
                IF EXISTS (SELECT 1 FROM [Departments] WHERE [DepartmentName] = N'Training' AND [IsDeleted] = 0)
                    UPDATE [Departments] SET [DepartmentCode] = N'TRN', [IsTrainingAudience] = 0, [Description] = COALESCE(NULLIF([Description], N''), N'Phòng Quản lý Đào tạo & Khảo thí')
                    WHERE [DepartmentId] = (SELECT MIN([DepartmentId]) FROM [Departments] WHERE [DepartmentName] = N'Training' AND [IsDeleted] = 0);
                ELSE IF NOT EXISTS (SELECT 1 FROM [Departments] WHERE [DepartmentCode] = N'TRN' AND [IsDeleted] = 0)
                    INSERT INTO [Departments] ([DepartmentName], [DepartmentCode], [Description], [IsTrainingAudience], [CreatedAt], [IsDeleted])
                    VALUES (N'Training', N'TRN', N'Phòng Quản lý Đào tạo & Khảo thí', 0, GETUTCDATE(), 0);

                -- Flight Crew (Training Audience: Phi cong)
                IF EXISTS (SELECT 1 FROM [Departments] WHERE [DepartmentName] = N'Flight Crew' AND [IsDeleted] = 0)
                    UPDATE [Departments] SET [DepartmentCode] = N'FC', [IsTrainingAudience] = 1, [Description] = COALESCE(NULLIF([Description], N''), N'Khoa / Đoàn Phi công (Flight Operations)')
                    WHERE [DepartmentId] = (SELECT MIN([DepartmentId]) FROM [Departments] WHERE [DepartmentName] = N'Flight Crew' AND [IsDeleted] = 0);
                ELSE IF NOT EXISTS (SELECT 1 FROM [Departments] WHERE [DepartmentCode] = N'FC' AND [IsDeleted] = 0)
                    INSERT INTO [Departments] ([DepartmentName], [DepartmentCode], [Description], [IsTrainingAudience], [CreatedAt], [IsDeleted])
                    VALUES (N'Flight Crew', N'FC', N'Khoa / Đoàn Phi công (Flight Operations)', 1, GETUTCDATE(), 0);

                -- Cabin Crew (Training Audience: Tiep vien)
                IF EXISTS (SELECT 1 FROM [Departments] WHERE [DepartmentName] = N'Cabin Crew' AND [IsDeleted] = 0)
                    UPDATE [Departments] SET [DepartmentCode] = N'CC', [IsTrainingAudience] = 1, [Description] = COALESCE(NULLIF([Description], N''), N'Khoa / Đoàn Tiếp viên hàng không (In-Flight Services)')
                    WHERE [DepartmentId] = (SELECT MIN([DepartmentId]) FROM [Departments] WHERE [DepartmentName] = N'Cabin Crew' AND [IsDeleted] = 0);
                ELSE IF NOT EXISTS (SELECT 1 FROM [Departments] WHERE [DepartmentCode] = N'CC' AND [IsDeleted] = 0)
                    INSERT INTO [Departments] ([DepartmentName], [DepartmentCode], [Description], [IsTrainingAudience], [CreatedAt], [IsDeleted])
                    VALUES (N'Cabin Crew', N'CC', N'Khoa / Đoàn Tiếp viên hàng không (In-Flight Services)', 1, GETUTCDATE(), 0);

                -- Engineering & Maintenance (Training Audience: Bao duong tau bay)
                IF EXISTS (SELECT 1 FROM [Departments] WHERE [DepartmentName] = N'Engineering & Maintenance' AND [IsDeleted] = 0)
                    UPDATE [Departments] SET [DepartmentCode] = N'ENG', [IsTrainingAudience] = 1, [Description] = COALESCE(NULLIF([Description], N''), N'Khoa Kỹ thuật & Bảo dưỡng tàu bay')
                    WHERE [DepartmentId] = (SELECT MIN([DepartmentId]) FROM [Departments] WHERE [DepartmentName] = N'Engineering & Maintenance' AND [IsDeleted] = 0);
                ELSE IF NOT EXISTS (SELECT 1 FROM [Departments] WHERE [DepartmentCode] = N'ENG' AND [IsDeleted] = 0)
                    INSERT INTO [Departments] ([DepartmentName], [DepartmentCode], [Description], [IsTrainingAudience], [CreatedAt], [IsDeleted])
                    VALUES (N'Engineering & Maintenance', N'ENG', N'Khoa Kỹ thuật & Bảo dưỡng tàu bay', 1, GETUTCDATE(), 0);

                -- Ground Operations (Training Audience: Nhan vien mat dat)
                IF EXISTS (SELECT 1 FROM [Departments] WHERE [DepartmentName] = N'Ground Operations' AND [IsDeleted] = 0)
                    UPDATE [Departments] SET [DepartmentCode] = N'GND', [IsTrainingAudience] = 1, [Description] = COALESCE(NULLIF([Description], N''), N'Khoa Khai thác mặt đất & Dịch vụ sân đỗ')
                    WHERE [DepartmentId] = (SELECT MIN([DepartmentId]) FROM [Departments] WHERE [DepartmentName] = N'Ground Operations' AND [IsDeleted] = 0);
                ELSE IF NOT EXISTS (SELECT 1 FROM [Departments] WHERE [DepartmentCode] = N'GND' AND [IsDeleted] = 0)
                    INSERT INTO [Departments] ([DepartmentName], [DepartmentCode], [Description], [IsTrainingAudience], [CreatedAt], [IsDeleted])
                    VALUES (N'Ground Operations', N'GND', N'Khoa Khai thác mặt đất & Dịch vụ sân đỗ', 1, GETUTCDATE(), 0);

                -- 2.4 Deduplicate any duplicate codes among active departments (keep lowest Id, rename duplicate to DEPT-{DepartmentId} which is intrinsically unique)
                ;WITH DupCte AS (
                    SELECT [DepartmentId], [DepartmentCode],
                           ROW_NUMBER() OVER (PARTITION BY [DepartmentCode] ORDER BY [DepartmentId]) AS rn
                    FROM [Departments]
                    WHERE [DepartmentCode] IS NOT NULL AND [DepartmentCode] <> N'' AND [IsDeleted] = 0
                )
                UPDATE d
                SET [DepartmentCode] = CONCAT(N'DEPT-', d.[DepartmentId])
                FROM [Departments] d
                INNER JOIN DupCte c ON d.[DepartmentId] = c.[DepartmentId]
                WHERE c.rn > 1;
            ");

            // 3. Create unique filtered index on DepartmentCode
            migrationBuilder.CreateIndex(
                name: "IX_Departments_DepartmentCode",
                table: "Departments",
                column: "DepartmentCode",
                unique: true,
                filter: "[DepartmentCode] IS NOT NULL AND [DepartmentCode] <> '' AND [IsDeleted] = 0");

            // 4. Create CourseDepartments join table
            migrationBuilder.CreateTable(
                name: "CourseDepartments",
                columns: table => new
                {
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByAccountId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByAccountId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseDepartments", x => new { x.CourseId, x.DepartmentId });
                    table.ForeignKey(
                        name: "FK_CourseDepartments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseDepartments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseDepartments_DepartmentId",
                table: "CourseDepartments",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseDepartments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_DepartmentCode",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "DepartmentCode",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "IsTrainingAudience",
                table: "Departments");
        }
    }
}
