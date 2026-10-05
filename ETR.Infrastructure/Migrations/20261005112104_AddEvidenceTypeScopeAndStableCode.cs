using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceTypeScopeAndStableCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add new columns
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "EvidenceTypes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "SubjectEvidence");

            migrationBuilder.AddColumn<string>(
                name: "DepartmentScope",
                table: "EvidenceTypes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMandatory",
                table: "EvidenceTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SubjectTypeScope",
                table: "EvidenceTypes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypeCode",
                table: "EvidenceTypes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // 2. Data cleansing, seed, backfill, and collision resolution (executed BEFORE creating unique index)
            migrationBuilder.Sql(@"
                -- 2.1 Backfill stable TypeCode for existing core IDs referenced by existing records
                IF EXISTS (SELECT 1 FROM [EvidenceTypes] WHERE [EvidenceTypeId] = 1)
                    UPDATE [EvidenceTypes]
                    SET [TypeCode] = N'AML',
                        [DepartmentScope] = N'ENG',
                        [SubjectTypeScope] = N'Practical',
                        [IsMandatory] = 0,
                        [Category] = N'SubjectEvidence'
                    WHERE [EvidenceTypeId] = 1;

                IF EXISTS (SELECT 1 FROM [EvidenceTypes] WHERE [EvidenceTypeId] = 2)
                    UPDATE [EvidenceTypes]
                    SET [TypeCode] = N'SIM_LOG',
                        [DepartmentScope] = N'FC',
                        [SubjectTypeScope] = N'Practical',
                        [IsMandatory] = 0,
                        [Category] = N'SubjectEvidence'
                    WHERE [EvidenceTypeId] = 2;

                IF EXISTS (SELECT 1 FROM [EvidenceTypes] WHERE [EvidenceTypeId] = 3)
                    UPDATE [EvidenceTypes]
                    SET [TypeCode] = N'EXAM_SCORE',
                        [DepartmentScope] = N'ALL',
                        [SubjectTypeScope] = N'Theory',
                        [IsMandatory] = 0,
                        [Category] = N'SubjectEvidence'
                    WHERE [EvidenceTypeId] = 3;

                IF EXISTS (SELECT 1 FROM [EvidenceTypes] WHERE [EvidenceTypeId] = 4)
                    UPDATE [EvidenceTypes]
                    SET [TypeCode] = N'MED_ELP',
                        [DepartmentScope] = N'ALL',
                        [SubjectTypeScope] = N'ALL',
                        [IsMandatory] = 0,
                        [Category] = N'Credential'
                    WHERE [EvidenceTypeId] = 4;

                -- 2.2 Upsert standard types by TypeName / TypeCode
                MERGE INTO [EvidenceTypes] AS target
                USING (VALUES
                    (N'AML', N'Aircraft Maintenance Log (AML)', N'Nhật ký kỹ thuật và sửa chữa, bảo dưỡng đường dài (Line Maintenance) của tàu bay.', N'ENG', N'Practical', 0, N'SubjectEvidence'),
                    (N'SIM_LOG', N'Flight Simulator Session Log', N'Bản in certified kết quả bài tập buồng lái mô phỏng SIM Level D (thông số bay, tiếp cận ILS, khẩn nguy).', N'FC', N'Practical', 0, N'SubjectEvidence'),
                    (N'EXAM_SCORE', N'Theory Exam Score Sheet', N'Bảng điểm bài thi lý thuyết có chữ ký giám thị và hội đồng chấm thi.', N'ALL', N'Theory', 0, N'SubjectEvidence'),
                    (N'MED_ELP', N'Medical & English Proficiency', N'Giấy chứng nhận sức khỏe hàng không CAAV Class 1/2 và chứng chỉ tiếng Anh ICAO Level 4+. Hồ sơ pháp lý nhạy cảm lưu riêng.', N'ALL', N'ALL', 0, N'Credential'),
                    (N'CHECK_RIDE', N'Check-Ride & Skill Test Assessment Form', N'Biên bản kiểm tra kỹ năng bay định kỳ/chuyển loại do Giám khảo bay (DPE/CAAV Inspector) phê chuẩn và ký tên.', N'FC', N'Practical', 0, N'SubjectEvidence'),
                    (N'LINE_CHECK', N'Route / Line Check Evaluation Report', N'Phiếu đánh giá bay trên tuyến thực tế và huấn luyện bay định hướng tuyến (LOFT / Line Check).', N'FC', N'Practical', 0, N'SubjectEvidence'),
                    (N'PILOT_LOG', N'Pilot Flight Logbook Endorsement', N'Trang trích lục sổ nhật ký bay huấn luyện có xác nhận của giáo viên bay (TRI/TRE).', N'FC', N'Practical', 0, N'SubjectEvidence'),
                    (N'OJT_LOG', N'Practical OJT Task Sign-off Sheet', N'Sổ nhật ký thực hành bảo dưỡng tại chỗ (On-the-Job Training) của kỹ sư bảo dưỡng AME.', N'ENG', N'Practical', 0, N'SubjectEvidence'),
                    (N'CRS_SIGN', N'Certificate of Release to Service (CRS) Evidence', N'Minh chứng bài thực hành kiểm tra và cấp chứng chỉ phê chuẩn phát hành bay an toàn.', N'ENG', N'Practical', 0, N'SubjectEvidence'),
                    (N'SEP_RECORD', N'Cabin Safety & Emergency Procedures (SEP) Record', N'Biên bản thực hành an toàn khẩn nguy (sơ tán 90 giây, mở cửa trượt thoát hiểm, hạ cánh trên nước Ditching).', N'CC', N'Practical', 0, N'SubjectEvidence'),
                    (N'AVMED_RECORD', N'Aviation Medicine & In-Flight First Aid Assessment', N'Biên bản kiểm tra thực hành sơ cấp cứu và hồi sinh tim phổi (CPR/AED) trên độ cao tuần tiễu.', N'CC', N'Practical', 0, N'SubjectEvidence'),
                    (N'FIRE_DRILL', N'Fire Fighting & Smoke Drill Evaluation Sheet', N'Biên bản đánh giá diễn tập dập lửa và xử lý khói độc trong khoang hành khách.', N'CC', N'Practical', 0, N'SubjectEvidence'),
                    (N'DGR_CHECK', N'Dangerous Goods Regulations (DGR) Practical Checklist', N'Biên bản kiểm tra thực hành đóng gói, dán nhãn và lập tờ khai hàng nguy hiểm IATA DGR Cat 6.', N'GND', N'ALL', 0, N'SubjectEvidence'),
                    (N'OFP_DISPATCH', N'Operational Flight Plan (OFP) & Dispatch Release', N'Bản kế hoạch bay điều độ và biên bản phát hành bay do Sĩ quan Điều hành bay (FOO) phê duyệt.', N'GND', N'ALL', 0, N'SubjectEvidence'),
                    (N'LOAD_SHEET', N'Weight & Balance / Load Sheet Calculation Form', N'Bảng tính toán và xác nhận cân bằng trọng tải tàu bay trước khi khởi hành.', N'GND', N'Practical', 0, N'SubjectEvidence'),
                    (N'RAMP_SAFETY', N'Ramp Safety & Aircraft Marshalling Sign-off', N'Biên bản sát hạch thực địa an toàn sân đỗ, kéo đẩy tàu bay (Pushback) và đánh tín hiệu tiếp cận bến đỗ.', N'GND', N'Practical', 0, N'SubjectEvidence'),
                    (N'ATTENDANCE_SHEET', N'Classroom Attendance & Roll Call Sheet', N'Bảng điểm danh thời lượng học tập trung có xác nhận của giảng viên đứng lớp.', N'ALL', N'ALL', 0, N'SubjectEvidence'),
                    (N'REMEDIAL_ENDORSE', N'Remedial & Retake Training Endorsement', N'Biên bản xác nhận hoàn thành huấn luyện bổ sung và thi lại sau khi không đạt bài kiểm tra lần đầu.', N'ALL', N'ALL', 0, N'SubjectEvidence'),
                    (N'OTHER_EVIDENCE', N'Other Practical / Training Evidence', N'Minh chứng đào tạo hoặc tài liệu đánh giá thực hành khác theo chỉ định của giảng viên.', N'ALL', N'ALL', 0, N'SubjectEvidence')
                ) AS src (TypeCode, TypeName, Description, DepartmentScope, SubjectTypeScope, IsMandatory, Category)
                ON (target.TypeCode = src.TypeCode OR target.TypeName = src.TypeName)
                WHEN MATCHED THEN
                    UPDATE SET
                        target.TypeCode = src.TypeCode,
                        target.TypeName = src.TypeName,
                        target.Description = src.Description,
                        target.DepartmentScope = src.DepartmentScope,
                        target.SubjectTypeScope = src.SubjectTypeScope,
                        target.IsMandatory = src.IsMandatory,
                        target.Category = src.Category,
                        target.IsDeleted = 0,
                        target.UpdatedAt = GETUTCDATE()
                WHEN NOT MATCHED THEN
                    INSERT (TypeCode, TypeName, Description, DepartmentScope, SubjectTypeScope, IsMandatory, Category, CreatedAt, IsDeleted)
                    VALUES (src.TypeCode, src.TypeName, src.Description, src.DepartmentScope, src.SubjectTypeScope, src.IsMandatory, src.Category, GETUTCDATE(), 0);

                -- 2.3 Backfill any remaining empty or whitespace TypeCode
                UPDATE [EvidenceTypes]
                SET [TypeCode] = CONCAT(N'EV-', [EvidenceTypeId])
                WHERE ([TypeCode] IS NULL OR [TypeCode] = N'' OR LTRIM(RTRIM([TypeCode])) = N'')
                  AND [IsDeleted] = 0;
            ");

            // 3. Create unique filtered index for TypeCode
            migrationBuilder.CreateIndex(
                name: "IX_EvidenceTypes_TypeCode",
                table: "EvidenceTypes",
                column: "TypeCode",
                unique: true,
                filter: "[TypeCode] IS NOT NULL AND [TypeCode] <> '' AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EvidenceTypes_TypeCode",
                table: "EvidenceTypes");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "EvidenceTypes");

            migrationBuilder.DropColumn(
                name: "DepartmentScope",
                table: "EvidenceTypes");

            migrationBuilder.DropColumn(
                name: "IsMandatory",
                table: "EvidenceTypes");

            migrationBuilder.DropColumn(
                name: "SubjectTypeScope",
                table: "EvidenceTypes");

            migrationBuilder.DropColumn(
                name: "TypeCode",
                table: "EvidenceTypes");
        }
    }
}
