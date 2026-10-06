using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ETR.Infrastructure.Data;

public static class DataSeeder
{
    private const string AdminUsername = "admin@etr.com";
    private const string StudentUsername = "student@etr.com";
    private const string InstructorUsername = "instructor@etr.com";
    private const string QaUsername = "qa@etr.com";
    private const string ManagerUsername = "manager@etr.com";
    private const string ManagementViewerUsername = "management-viewer@etr.com";

    public static async Task SeedAsync(AppDbContext context)
    {
        await SeedIdentityAsync(context);
        await SeedCatalogAsync(context);
        await SeedTrainingFacilitiesAsync(context);
        await SeedClassSchedulingAsync(context);
        await SeedEnrollmentAsync(context);
        await SeedEtrAndSubjectResultsAsync(context);
        await SeedAttendanceAsync(context);
        await SeedAssessmentResultsAsync(context);
        await SeedPracticalChecklistResultsAsync(context);
        await SeedSignoffAsync(context);
        await SeedEvidenceAsync(context);
        await SeedApprovalWorkflowAsync(context);
        await SeedAmendmentRequestsAsync(context);
        await SeedMiscellaneousAsync(context);
    }

    private static async Task SeedIdentityAsync(AppDbContext context)
    {
        if (!await context.Roles.AnyAsync())
        {
            context.Roles.AddRange(
                new Role { RoleName = "Admin", Description = "System Administrator" },
                new Role { RoleName = "Instructor", Description = "Course Instructor" },
                new Role { RoleName = "QA", Description = "Quality Assurance" },
                new Role { RoleName = "Academic", Description = "Academic Staff" },
                new Role { RoleName = "TrainingManager", Description = "Training Manager" },
                new Role { RoleName = "Student", Description = "Student / Learner" },
                new Role { RoleName = "Audit", Description = "Auditor" },
                new Role { RoleName = "ManagementViewer", Description = "Management Viewer" });
            await context.SaveChangesAsync();
        }

        var defaultDepartments = new[]
        {
            new Department { DepartmentName = "Administration", DepartmentCode = "ADM", Description = "Ban giám hiệu & Quản trị hệ thống", IsTrainingAudience = false },
            new Department { DepartmentName = "Training", DepartmentCode = "TRN", Description = "Phòng Quản lý Đào tạo & Khảo thí", IsTrainingAudience = false },
            new Department { DepartmentName = "Flight Crew", DepartmentCode = "FC", Description = "Khoa / Đoàn Phi công (Flight Operations)", IsTrainingAudience = true },
            new Department { DepartmentName = "Cabin Crew", DepartmentCode = "CC", Description = "Khoa / Đoàn Tiếp viên hàng không (In-Flight Services)", IsTrainingAudience = true },
            new Department { DepartmentName = "Engineering & Maintenance", DepartmentCode = "ENG", Description = "Khoa Kỹ thuật & Bảo dưỡng tàu bay", IsTrainingAudience = true },
            new Department { DepartmentName = "Ground Operations", DepartmentCode = "GND", Description = "Khoa Khai thác mặt đất & Dịch vụ sân đỗ", IsTrainingAudience = true }
        };
        foreach (var dept in defaultDepartments)
        {
            var existing = await context.Departments.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.DepartmentName == dept.DepartmentName);
            if (existing == null)
            {
                context.Departments.Add(dept);
            }
            else
            {
                if (string.IsNullOrEmpty(existing.DepartmentCode)) existing.DepartmentCode = dept.DepartmentCode;
                existing.IsTrainingAudience = dept.IsTrainingAudience;
                if (string.IsNullOrEmpty(existing.Description)) existing.Description = dept.Description;
                context.Departments.Update(existing);
            }
        }
        await context.SaveChangesAsync();

        if (!await context.Accounts.AnyAsync())
        {
            var roleIds = await context.Roles.ToDictionaryAsync(r => r.RoleName, r => r.RoleId);
            var deptIds = await context.Departments.ToDictionaryAsync(d => d.DepartmentName, d => d.DepartmentId);
            var pwd = BCrypt.Net.BCrypt.HashPassword("123456");

            var accounts = new List<Account>
            {
                new Account { Username = AdminUsername, PasswordHash = pwd, RoleId = roleIds["Admin"], DepartmentId = deptIds["Administration"], Status = AccountStatus.Active, Profile = new UserProfile { UserCode = "ADM-01", FullName = "System Admin", Email = AdminUsername } },
                new Account { Username = InstructorUsername, PasswordHash = pwd, RoleId = roleIds["Instructor"], DepartmentId = deptIds["Training"], Status = AccountStatus.Active, Profile = new UserProfile { UserCode = "INS-01", FullName = "Senior Instructor", Email = InstructorUsername } },
                new Account { Username = QaUsername, PasswordHash = pwd, RoleId = roleIds["QA"], DepartmentId = deptIds["Administration"], Status = AccountStatus.Active, Profile = new UserProfile { UserCode = "QA-01", FullName = "QA Specialist", Email = QaUsername } },
                new Account { Username = ManagerUsername, PasswordHash = pwd, RoleId = roleIds["TrainingManager"], DepartmentId = deptIds["Training"], Status = AccountStatus.Active, Profile = new UserProfile { UserCode = "MGR-01", FullName = "Training Manager", Email = ManagerUsername } },
                new Account { Username = StudentUsername, PasswordHash = pwd, RoleId = roleIds["Student"], DepartmentId = deptIds["Training"], Status = AccountStatus.Active, Profile = new UserProfile { UserCode = "STU-01", FullName = "Jane Student", Email = StudentUsername } },
                new Account { Username = ManagementViewerUsername, PasswordHash = pwd, RoleId = roleIds["ManagementViewer"], DepartmentId = deptIds["Administration"], Status = AccountStatus.Active, Profile = new UserProfile { UserCode = "MGV-01", FullName = "Management Viewer", Email = ManagementViewerUsername } },
                new Account { Username = "academic@etr.com", PasswordHash = pwd, RoleId = roleIds["Academic"], DepartmentId = deptIds["Administration"], Status = AccountStatus.Active, Profile = new UserProfile { UserCode = "ACA-01", FullName = "Academic Staff", Email = "academic@etr.com" } },
                new Account { Username = "audit@etr.com", PasswordHash = pwd, RoleId = roleIds["Audit"], DepartmentId = deptIds["Administration"], Status = AccountStatus.Active, Profile = new UserProfile { UserCode = "AUD-01", FullName = "Audit Staff", Email = "audit@etr.com" } }
            };

            // Mass seed students
            for(int i=2; i<=30; i++) {
                accounts.Add(new Account { Username = $"student{i}@etr.com", PasswordHash = pwd, RoleId = roleIds["Student"], DepartmentId = deptIds["Training"], Status = AccountStatus.Active, Profile = new UserProfile { UserCode = $"STU-{i:00}", FullName = $"Student {i}", Email = $"student{i}@etr.com" } });
            }
            // Mass seed instructors
            for(int i=2; i<=10; i++) {
                accounts.Add(new Account { Username = $"instructor{i}@etr.com", PasswordHash = pwd, RoleId = roleIds["Instructor"], DepartmentId = deptIds["Training"], Status = AccountStatus.Active, Profile = new UserProfile { UserCode = $"INS-{i:00}", FullName = $"Instructor {i}", Email = $"instructor{i}@etr.com" } });
            }

            context.Accounts.AddRange(accounts);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedCatalogAsync(AppDbContext context)
    {
        if (!await context.Courses.AnyAsync())
        {
            context.Courses.Add(new Course { CourseCode = "AMT-101", CourseName = "Aircraft Maintenance Technician", DurationHours = 120, Status = CourseStatus.Active, ValidityMonths = 24 });
            context.Courses.Add(new Course { CourseCode = "B737-TR", CourseName = "B737 Type Rating", DurationHours = 160, Status = CourseStatus.Active, ValidityMonths = 12 });
            context.Courses.Add(new Course { CourseCode = "A320-FAM", CourseName = "A320 Familiarization", DurationHours = 40, Status = CourseStatus.Active, ValidityMonths = 24 });
            context.Courses.Add(new Course { CourseCode = "ENG-101", CourseName = "Aviation English", DurationHours = 60, Status = CourseStatus.Active, ValidityMonths = 36 });
            context.Courses.Add(new Course { CourseCode = "SMS-101", CourseName = "Safety Management Systems", DurationHours = 20, Status = CourseStatus.Active, ValidityMonths = 12 });
            context.Courses.Add(new Course { CourseCode = "HF-101", CourseName = "Human Factors", DurationHours = 15, Status = CourseStatus.Active, ValidityMonths = 24 });
            context.Courses.Add(new Course { CourseCode = "A350-TR", CourseName = "A350 Type Rating", DurationHours = 160, Status = CourseStatus.Active, ValidityMonths = 12 });
            context.Courses.Add(new Course { CourseCode = "B787-TR", CourseName = "B787 Type Rating", DurationHours = 160, Status = CourseStatus.Active, ValidityMonths = 12 });
            context.Courses.Add(new Course { CourseCode = "DGR-101", CourseName = "Dangerous Goods Regulations", DurationHours = 10, Status = CourseStatus.Active, ValidityMonths = 12 });
            context.Courses.Add(new Course { CourseCode = "SEC-101", CourseName = "Aviation Security", DurationHours = 10, Status = CourseStatus.Active, ValidityMonths = 24 });
            await context.SaveChangesAsync();
        }

        if (!await context.Subjects.AnyAsync())
        {
            context.Subjects.Add(new Subject { SubjectCode = "SJ-REG", SubjectName = "Aviation Regulations", SubjectType = "Theory", Status = SubjectStatus.Active });
            context.Subjects.Add(new Subject { SubjectCode = "SJ-SYS", SubjectName = "Aircraft Systems", SubjectType = "Theory", Status = SubjectStatus.Active });
            context.Subjects.Add(new Subject { SubjectCode = "SJ-PRA", SubjectName = "Practical Maintenance", SubjectType = "Practical", Status = SubjectStatus.Active });
            context.Subjects.Add(new Subject { SubjectCode = "SJ-SAF", SubjectName = "Safety & Human Factors", SubjectType = "Theory", Status = SubjectStatus.Active });
            context.Subjects.Add(new Subject { SubjectCode = "SJ-ENG", SubjectName = "Technical English", SubjectType = "Theory", Status = SubjectStatus.Active });
            await context.SaveChangesAsync();
        }

        if (!await context.CourseSubjects.AnyAsync())
        {
            var courses = await context.Courses.ToDictionaryAsync(c => c.CourseCode, c => c.CourseId);
            var subjects = await context.Subjects.ToDictionaryAsync(s => s.SubjectCode, s => s.SubjectId);
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["AMT-101"], SubjectId = subjects["SJ-REG"], SequenceNo = 1, RequiredHours = 10, RequiredSessions = 3, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["AMT-101"], SubjectId = subjects["SJ-SYS"], SequenceNo = 2, RequiredHours = 10, RequiredSessions = 3, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["AMT-101"], SubjectId = subjects["SJ-PRA"], SequenceNo = 3, RequiredHours = 10, RequiredSessions = 3, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["AMT-101"], SubjectId = subjects["SJ-SAF"], SequenceNo = 4, RequiredHours = 10, RequiredSessions = 2, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["AMT-101"], SubjectId = subjects["SJ-ENG"], SequenceNo = 5, RequiredHours = 10, RequiredSessions = 2, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["B737-TR"], SubjectId = subjects["SJ-REG"], SequenceNo = 1, RequiredHours = 10, RequiredSessions = 3, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["B737-TR"], SubjectId = subjects["SJ-SYS"], SequenceNo = 2, RequiredHours = 10, RequiredSessions = 3, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["B737-TR"], SubjectId = subjects["SJ-PRA"], SequenceNo = 3, RequiredHours = 10, RequiredSessions = 3, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["B737-TR"], SubjectId = subjects["SJ-SAF"], SequenceNo = 4, RequiredHours = 10, RequiredSessions = 2, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["B737-TR"], SubjectId = subjects["SJ-ENG"], SequenceNo = 5, RequiredHours = 10, RequiredSessions = 2, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["A320-FAM"], SubjectId = subjects["SJ-REG"], SequenceNo = 1, RequiredHours = 10, RequiredSessions = 2, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["A320-FAM"], SubjectId = subjects["SJ-SYS"], SequenceNo = 2, RequiredHours = 10, RequiredSessions = 2, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["A320-FAM"], SubjectId = subjects["SJ-PRA"], SequenceNo = 3, RequiredHours = 10, RequiredSessions = 2, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["A320-FAM"], SubjectId = subjects["SJ-SAF"], SequenceNo = 4, RequiredHours = 10, RequiredSessions = 2, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["A320-FAM"], SubjectId = subjects["SJ-ENG"], SequenceNo = 5, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["ENG-101"], SubjectId = subjects["SJ-ENG"], SequenceNo = 1, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["SMS-101"], SubjectId = subjects["SJ-REG"], SequenceNo = 1, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["SMS-101"], SubjectId = subjects["SJ-SYS"], SequenceNo = 2, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["SMS-101"], SubjectId = subjects["SJ-PRA"], SequenceNo = 3, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["SMS-101"], SubjectId = subjects["SJ-SAF"], SequenceNo = 4, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["SMS-101"], SubjectId = subjects["SJ-ENG"], SequenceNo = 5, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["HF-101"], SubjectId = subjects["SJ-REG"], SequenceNo = 1, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["HF-101"], SubjectId = subjects["SJ-SYS"], SequenceNo = 2, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["HF-101"], SubjectId = subjects["SJ-PRA"], SequenceNo = 3, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["HF-101"], SubjectId = subjects["SJ-SAF"], SequenceNo = 4, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["HF-101"], SubjectId = subjects["SJ-ENG"], SequenceNo = 5, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["A350-TR"], SubjectId = subjects["SJ-REG"], SequenceNo = 1, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["A350-TR"], SubjectId = subjects["SJ-SYS"], SequenceNo = 2, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["A350-TR"], SubjectId = subjects["SJ-PRA"], SequenceNo = 3, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["A350-TR"], SubjectId = subjects["SJ-SAF"], SequenceNo = 4, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["A350-TR"], SubjectId = subjects["SJ-ENG"], SequenceNo = 5, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["B787-TR"], SubjectId = subjects["SJ-REG"], SequenceNo = 1, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["B787-TR"], SubjectId = subjects["SJ-SYS"], SequenceNo = 2, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["B787-TR"], SubjectId = subjects["SJ-PRA"], SequenceNo = 3, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["B787-TR"], SubjectId = subjects["SJ-SAF"], SequenceNo = 4, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["B787-TR"], SubjectId = subjects["SJ-ENG"], SequenceNo = 5, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["DGR-101"], SubjectId = subjects["SJ-REG"], SequenceNo = 1, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["DGR-101"], SubjectId = subjects["SJ-SYS"], SequenceNo = 2, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["DGR-101"], SubjectId = subjects["SJ-PRA"], SequenceNo = 3, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["DGR-101"], SubjectId = subjects["SJ-SAF"], SequenceNo = 4, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["DGR-101"], SubjectId = subjects["SJ-ENG"], SequenceNo = 5, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["SEC-101"], SubjectId = subjects["SJ-REG"], SequenceNo = 1, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["SEC-101"], SubjectId = subjects["SJ-SYS"], SequenceNo = 2, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["SEC-101"], SubjectId = subjects["SJ-PRA"], SequenceNo = 3, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["SEC-101"], SubjectId = subjects["SJ-SAF"], SequenceNo = 4, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["SEC-101"], SubjectId = subjects["SJ-ENG"], SequenceNo = 5, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            await context.SaveChangesAsync();
        }

        if (!await context.CompletionRequirements.AnyAsync())
        {
            var courses = await context.Courses.ToDictionaryAsync(c => c.CourseCode, c => c.CourseId);
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["AMT-101"], RequirementName = "Minimum 80% Attendance", IsMandatory = true, DisplayOrder = 1, RequirementType = "MinAttendance", ThresholdValue = 80m });
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["AMT-101"], RequirementName = "All Assessments Passed", IsMandatory = true, DisplayOrder = 2, RequirementType = "AllAssessmentsPassed" });
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["B737-TR"], RequirementName = "Minimum 80% Attendance", IsMandatory = true, DisplayOrder = 1, RequirementType = "MinAttendance", ThresholdValue = 80m });
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["B737-TR"], RequirementName = "All Assessments Passed", IsMandatory = true, DisplayOrder = 2, RequirementType = "AllAssessmentsPassed" });
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["A320-FAM"], RequirementName = "Minimum 80% Attendance", IsMandatory = true, DisplayOrder = 1, RequirementType = "MinAttendance", ThresholdValue = 80m });
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["A320-FAM"], RequirementName = "All Assessments Passed", IsMandatory = true, DisplayOrder = 2, RequirementType = "AllAssessmentsPassed" });
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["ENG-101"], RequirementName = "Minimum 80% Attendance", IsMandatory = true, DisplayOrder = 1, RequirementType = "MinAttendance", ThresholdValue = 80m });
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["ENG-101"], RequirementName = "All Assessments Passed", IsMandatory = true, DisplayOrder = 2, RequirementType = "AllAssessmentsPassed" });
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["SMS-101"], RequirementName = "Minimum 80% Attendance", IsMandatory = true, DisplayOrder = 1, RequirementType = "MinAttendance", ThresholdValue = 80m });
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["SMS-101"], RequirementName = "All Assessments Passed", IsMandatory = true, DisplayOrder = 2, RequirementType = "AllAssessmentsPassed" });
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["HF-101"], RequirementName = "Minimum 80% Attendance", IsMandatory = true, DisplayOrder = 1, RequirementType = "MinAttendance", ThresholdValue = 80m });
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["HF-101"], RequirementName = "All Assessments Passed", IsMandatory = true, DisplayOrder = 2, RequirementType = "AllAssessmentsPassed" });
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["A350-TR"], RequirementName = "Minimum 80% Attendance", IsMandatory = true, DisplayOrder = 1, RequirementType = "MinAttendance", ThresholdValue = 80m });
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["A350-TR"], RequirementName = "All Assessments Passed", IsMandatory = true, DisplayOrder = 2, RequirementType = "AllAssessmentsPassed" });
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["B787-TR"], RequirementName = "Minimum 80% Attendance", IsMandatory = true, DisplayOrder = 1, RequirementType = "MinAttendance", ThresholdValue = 80m });
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["B787-TR"], RequirementName = "All Assessments Passed", IsMandatory = true, DisplayOrder = 2, RequirementType = "AllAssessmentsPassed" });
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["DGR-101"], RequirementName = "Minimum 80% Attendance", IsMandatory = true, DisplayOrder = 1, RequirementType = "MinAttendance", ThresholdValue = 80m });
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["DGR-101"], RequirementName = "All Assessments Passed", IsMandatory = true, DisplayOrder = 2, RequirementType = "AllAssessmentsPassed" });
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["SEC-101"], RequirementName = "Minimum 80% Attendance", IsMandatory = true, DisplayOrder = 1, RequirementType = "MinAttendance", ThresholdValue = 80m });
            context.CompletionRequirements.Add(new CompletionRequirement { CourseId = courses["SEC-101"], RequirementName = "All Assessments Passed", IsMandatory = true, DisplayOrder = 2, RequirementType = "AllAssessmentsPassed" });
            await context.SaveChangesAsync();
        }

        if (!await context.Assessments.AnyAsync())
        {
            var courses = await context.Courses.ToDictionaryAsync(c => c.CourseCode, c => c.CourseId);
            var subjects = await context.Subjects.ToDictionaryAsync(s => s.SubjectCode, s => s.SubjectId);
            context.Assessments.Add(new Assessment { CourseId = courses["AMT-101"], SubjectId = subjects["SJ-REG"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["AMT-101"], SubjectId = subjects["SJ-SYS"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["AMT-101"], SubjectId = subjects["SJ-PRA"], ComponentName = "Final Exam", AssessmentType = "Practical", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["AMT-101"], SubjectId = subjects["SJ-SAF"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["AMT-101"], SubjectId = subjects["SJ-ENG"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["B737-TR"], SubjectId = subjects["SJ-REG"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["B737-TR"], SubjectId = subjects["SJ-SYS"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["B737-TR"], SubjectId = subjects["SJ-PRA"], ComponentName = "Final Exam", AssessmentType = "Practical", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["B737-TR"], SubjectId = subjects["SJ-SAF"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["B737-TR"], SubjectId = subjects["SJ-ENG"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["A320-FAM"], SubjectId = subjects["SJ-REG"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["A320-FAM"], SubjectId = subjects["SJ-SYS"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["A320-FAM"], SubjectId = subjects["SJ-PRA"], ComponentName = "Final Exam", AssessmentType = "Practical", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["A320-FAM"], SubjectId = subjects["SJ-SAF"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["A320-FAM"], SubjectId = subjects["SJ-ENG"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["ENG-101"], SubjectId = subjects["SJ-ENG"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["SMS-101"], SubjectId = subjects["SJ-REG"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["SMS-101"], SubjectId = subjects["SJ-SYS"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["SMS-101"], SubjectId = subjects["SJ-PRA"], ComponentName = "Final Exam", AssessmentType = "Practical", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["SMS-101"], SubjectId = subjects["SJ-SAF"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["SMS-101"], SubjectId = subjects["SJ-ENG"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["HF-101"], SubjectId = subjects["SJ-REG"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["HF-101"], SubjectId = subjects["SJ-SYS"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["HF-101"], SubjectId = subjects["SJ-PRA"], ComponentName = "Final Exam", AssessmentType = "Practical", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["HF-101"], SubjectId = subjects["SJ-SAF"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["HF-101"], SubjectId = subjects["SJ-ENG"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["A350-TR"], SubjectId = subjects["SJ-REG"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["A350-TR"], SubjectId = subjects["SJ-SYS"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["A350-TR"], SubjectId = subjects["SJ-PRA"], ComponentName = "Final Exam", AssessmentType = "Practical", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["A350-TR"], SubjectId = subjects["SJ-SAF"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["A350-TR"], SubjectId = subjects["SJ-ENG"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["B787-TR"], SubjectId = subjects["SJ-REG"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["B787-TR"], SubjectId = subjects["SJ-SYS"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["B787-TR"], SubjectId = subjects["SJ-PRA"], ComponentName = "Final Exam", AssessmentType = "Practical", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["B787-TR"], SubjectId = subjects["SJ-SAF"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["B787-TR"], SubjectId = subjects["SJ-ENG"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["DGR-101"], SubjectId = subjects["SJ-REG"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["DGR-101"], SubjectId = subjects["SJ-SYS"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["DGR-101"], SubjectId = subjects["SJ-PRA"], ComponentName = "Final Exam", AssessmentType = "Practical", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["DGR-101"], SubjectId = subjects["SJ-SAF"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["DGR-101"], SubjectId = subjects["SJ-ENG"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["SEC-101"], SubjectId = subjects["SJ-REG"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["SEC-101"], SubjectId = subjects["SJ-SYS"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["SEC-101"], SubjectId = subjects["SJ-PRA"], ComponentName = "Final Exam", AssessmentType = "Practical", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["SEC-101"], SubjectId = subjects["SJ-SAF"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            context.Assessments.Add(new Assessment { CourseId = courses["SEC-101"], SubjectId = subjects["SJ-ENG"], ComponentName = "Final Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            await context.SaveChangesAsync();
        }

        if (!await context.PracticalChecklists.AnyAsync())
        {
            var courses = await context.Courses.ToDictionaryAsync(c => c.CourseCode, c => c.CourseId);
            var subjects = await context.Subjects.ToDictionaryAsync(s => s.SubjectCode, s => s.SubjectId);
            context.PracticalChecklists.Add(new PracticalChecklist { CourseId = courses["AMT-101"], SubjectId = subjects["SJ-PRA"], ItemName = "Checklist 1", IsRequired = true, DisplayOrder = 1 });
            context.PracticalChecklists.Add(new PracticalChecklist { CourseId = courses["AMT-101"], SubjectId = subjects["SJ-PRA"], ItemName = "Checklist 2", IsRequired = true, DisplayOrder = 2 });
            context.PracticalChecklists.Add(new PracticalChecklist { CourseId = courses["B737-TR"], SubjectId = subjects["SJ-PRA"], ItemName = "Checklist 1", IsRequired = true, DisplayOrder = 1 });
            context.PracticalChecklists.Add(new PracticalChecklist { CourseId = courses["B737-TR"], SubjectId = subjects["SJ-PRA"], ItemName = "Checklist 2", IsRequired = true, DisplayOrder = 2 });
            context.PracticalChecklists.Add(new PracticalChecklist { CourseId = courses["A320-FAM"], SubjectId = subjects["SJ-PRA"], ItemName = "Checklist 1", IsRequired = true, DisplayOrder = 1 });
            context.PracticalChecklists.Add(new PracticalChecklist { CourseId = courses["A320-FAM"], SubjectId = subjects["SJ-PRA"], ItemName = "Checklist 2", IsRequired = true, DisplayOrder = 2 });
            context.PracticalChecklists.Add(new PracticalChecklist { CourseId = courses["SMS-101"], SubjectId = subjects["SJ-PRA"], ItemName = "Checklist 1", IsRequired = true, DisplayOrder = 1 });
            context.PracticalChecklists.Add(new PracticalChecklist { CourseId = courses["SMS-101"], SubjectId = subjects["SJ-PRA"], ItemName = "Checklist 2", IsRequired = true, DisplayOrder = 2 });
            context.PracticalChecklists.Add(new PracticalChecklist { CourseId = courses["HF-101"], SubjectId = subjects["SJ-PRA"], ItemName = "Checklist 1", IsRequired = true, DisplayOrder = 1 });
            context.PracticalChecklists.Add(new PracticalChecklist { CourseId = courses["HF-101"], SubjectId = subjects["SJ-PRA"], ItemName = "Checklist 2", IsRequired = true, DisplayOrder = 2 });
            context.PracticalChecklists.Add(new PracticalChecklist { CourseId = courses["A350-TR"], SubjectId = subjects["SJ-PRA"], ItemName = "Checklist 1", IsRequired = true, DisplayOrder = 1 });
            context.PracticalChecklists.Add(new PracticalChecklist { CourseId = courses["A350-TR"], SubjectId = subjects["SJ-PRA"], ItemName = "Checklist 2", IsRequired = true, DisplayOrder = 2 });
            context.PracticalChecklists.Add(new PracticalChecklist { CourseId = courses["B787-TR"], SubjectId = subjects["SJ-PRA"], ItemName = "Checklist 1", IsRequired = true, DisplayOrder = 1 });
            context.PracticalChecklists.Add(new PracticalChecklist { CourseId = courses["B787-TR"], SubjectId = subjects["SJ-PRA"], ItemName = "Checklist 2", IsRequired = true, DisplayOrder = 2 });
            context.PracticalChecklists.Add(new PracticalChecklist { CourseId = courses["DGR-101"], SubjectId = subjects["SJ-PRA"], ItemName = "Checklist 1", IsRequired = true, DisplayOrder = 1 });
            context.PracticalChecklists.Add(new PracticalChecklist { CourseId = courses["DGR-101"], SubjectId = subjects["SJ-PRA"], ItemName = "Checklist 2", IsRequired = true, DisplayOrder = 2 });
            context.PracticalChecklists.Add(new PracticalChecklist { CourseId = courses["SEC-101"], SubjectId = subjects["SJ-PRA"], ItemName = "Checklist 1", IsRequired = true, DisplayOrder = 1 });
            context.PracticalChecklists.Add(new PracticalChecklist { CourseId = courses["SEC-101"], SubjectId = subjects["SJ-PRA"], ItemName = "Checklist 2", IsRequired = true, DisplayOrder = 2 });
            await context.SaveChangesAsync();
        }

        if (!await context.EvidenceTypes.AnyAsync())
        {
            context.EvidenceTypes.AddRange(
                new EvidenceType { TypeCode = "AML", TypeName = "Aircraft Maintenance Log (AML)", Description = "Nhật ký kỹ thuật và sửa chữa, bảo dưỡng đường dài (Line Maintenance) của tàu bay.", DepartmentScope = "ENG", SubjectTypeScope = "Practical", IsMandatory = false, Category = "SubjectEvidence" },
                new EvidenceType { TypeCode = "SIM_LOG", TypeName = "Flight Simulator Session Log", Description = "Bản in certified kết quả bài tập buồng lái mô phỏng SIM Level D (thông số bay, tiếp cận ILS, khẩn nguy).", DepartmentScope = "FC", SubjectTypeScope = "Practical", IsMandatory = false, Category = "SubjectEvidence" },
                new EvidenceType { TypeCode = "EXAM_SCORE", TypeName = "Theory Exam Score Sheet", Description = "Bảng điểm bài thi lý thuyết có chữ ký giám thị và hội đồng chấm thi.", DepartmentScope = "ALL", SubjectTypeScope = "Theory", IsMandatory = false, Category = "SubjectEvidence" },
                new EvidenceType { TypeCode = "MED_ELP", TypeName = "Medical & English Proficiency", Description = "Giấy chứng nhận sức khỏe hàng không CAAV Class 1/2 và chứng chỉ tiếng Anh ICAO Level 4+. Hồ sơ pháp lý nhạy cảm lưu riêng.", DepartmentScope = "ALL", SubjectTypeScope = "ALL", IsMandatory = false, Category = "Credential" },
                new EvidenceType { TypeCode = "CHECK_RIDE", TypeName = "Check-Ride & Skill Test Assessment Form", Description = "Biên bản kiểm tra kỹ năng bay định kỳ/chuyển loại do Giám khảo bay (DPE/CAAV Inspector) phê chuẩn và ký tên.", DepartmentScope = "FC", SubjectTypeScope = "Practical", IsMandatory = false, Category = "SubjectEvidence" },
                new EvidenceType { TypeCode = "LINE_CHECK", TypeName = "Route / Line Check Evaluation Report", Description = "Phiếu đánh giá bay trên tuyến thực tế và huấn luyện bay định hướng tuyến (LOFT / Line Check).", DepartmentScope = "FC", SubjectTypeScope = "Practical", IsMandatory = false, Category = "SubjectEvidence" },
                new EvidenceType { TypeCode = "PILOT_LOG", TypeName = "Pilot Flight Logbook Endorsement", Description = "Trang trích lục sổ nhật ký bay huấn luyện có xác nhận của giáo viên bay (TRI/TRE).", DepartmentScope = "FC", SubjectTypeScope = "Practical", IsMandatory = false, Category = "SubjectEvidence" },
                new EvidenceType { TypeCode = "OJT_LOG", TypeName = "Practical OJT Task Sign-off Sheet", Description = "Sổ nhật ký thực hành bảo dưỡng tại chỗ (On-the-Job Training) của kỹ sư bảo dưỡng AME.", DepartmentScope = "ENG", SubjectTypeScope = "Practical", IsMandatory = false, Category = "SubjectEvidence" },
                new EvidenceType { TypeCode = "CRS_SIGN", TypeName = "Certificate of Release to Service (CRS) Evidence", Description = "Minh chứng bài thực hành kiểm tra và cấp chứng chỉ phê chuẩn phát hành bay an toàn.", DepartmentScope = "ENG", SubjectTypeScope = "Practical", IsMandatory = false, Category = "SubjectEvidence" },
                new EvidenceType { TypeCode = "SEP_RECORD", TypeName = "Cabin Safety & Emergency Procedures (SEP) Record", Description = "Biên bản thực hành an toàn khẩn nguy (sơ tán 90 giây, mở cửa trượt thoát hiểm, hạ cánh trên nước Ditching).", DepartmentScope = "CC", SubjectTypeScope = "Practical", IsMandatory = false, Category = "SubjectEvidence" },
                new EvidenceType { TypeCode = "AVMED_RECORD", TypeName = "Aviation Medicine & In-Flight First Aid Assessment", Description = "Biên bản kiểm tra thực hành sơ cấp cứu và hồi sinh tim phổi (CPR/AED) trên độ cao tuần tiễu.", DepartmentScope = "CC", SubjectTypeScope = "Practical", IsMandatory = false, Category = "SubjectEvidence" },
                new EvidenceType { TypeCode = "FIRE_DRILL", TypeName = "Fire Fighting & Smoke Drill Evaluation Sheet", Description = "Biên bản đánh giá diễn tập dập lửa và xử lý khói độc trong khoang hành khách.", DepartmentScope = "CC", SubjectTypeScope = "Practical", IsMandatory = false, Category = "SubjectEvidence" },
                new EvidenceType { TypeCode = "DGR_CHECK", TypeName = "Dangerous Goods Regulations (DGR) Practical Checklist", Description = "Biên bản kiểm tra thực hành đóng gói, dán nhãn và lập tờ khai hàng nguy hiểm IATA DGR Cat 6.", DepartmentScope = "GND", SubjectTypeScope = "ALL", IsMandatory = false, Category = "SubjectEvidence" },
                new EvidenceType { TypeCode = "OFP_DISPATCH", TypeName = "Operational Flight Plan (OFP) & Dispatch Release", Description = "Bản kế hoạch bay điều độ và biên bản phát hành bay do Sĩ quan Điều hành bay (FOO) phê duyệt.", DepartmentScope = "GND", SubjectTypeScope = "ALL", IsMandatory = false, Category = "SubjectEvidence" },
                new EvidenceType { TypeCode = "LOAD_SHEET", TypeName = "Weight & Balance / Load Sheet Calculation Form", Description = "Bảng tính toán và xác nhận cân bằng trọng tải tàu bay trước khi khởi hành.", DepartmentScope = "GND", SubjectTypeScope = "Practical", IsMandatory = false, Category = "SubjectEvidence" },
                new EvidenceType { TypeCode = "RAMP_SAFETY", TypeName = "Ramp Safety & Aircraft Marshalling Sign-off", Description = "Biên bản sát hạch thực địa an toàn sân đỗ, kéo đẩy tàu bay (Pushback) và đánh tín hiệu tiếp cận bến đỗ.", DepartmentScope = "GND", SubjectTypeScope = "Practical", IsMandatory = false, Category = "SubjectEvidence" },
                new EvidenceType { TypeCode = "ATTENDANCE_SHEET", TypeName = "Classroom Attendance & Roll Call Sheet", Description = "Bảng điểm danh thời lượng học tập trung có xác nhận của giảng viên đứng lớp.", DepartmentScope = "ALL", SubjectTypeScope = "ALL", IsMandatory = false, Category = "SubjectEvidence" },
                new EvidenceType { TypeCode = "REMEDIAL_ENDORSE", TypeName = "Remedial & Retake Training Endorsement", Description = "Biên bản xác nhận hoàn thành huấn luyện bổ sung và thi lại sau khi không đạt bài kiểm tra lần đầu.", DepartmentScope = "ALL", SubjectTypeScope = "ALL", IsMandatory = false, Category = "SubjectEvidence" },
                new EvidenceType { TypeCode = "OTHER_EVIDENCE", TypeName = "Other Practical / Training Evidence", Description = "Minh chứng đào tạo hoặc tài liệu đánh giá thực hành khác theo chỉ định của giảng viên.", DepartmentScope = "ALL", SubjectTypeScope = "ALL", IsMandatory = false, Category = "SubjectEvidence" });
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Dữ liệu mẫu cơ sở đào tạo / địa điểm (Training Facilities).
    /// Được đánh dấu là dữ liệu mẫu có thể cấu hình theo từng cơ sở thực tế (Sample Configurable Seed Data).
    /// </summary>
    private static async Task SeedTrainingFacilitiesAsync(AppDbContext context)
    {
        var existingCodes = await context.TrainingFacilities.Select(f => f.FacilityCode).ToListAsync();
        var existingSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

        var allFacilities = new List<TrainingFacility>
        {
            // 1. Phòng học lý thuyết (Ground Classrooms)
            new TrainingFacility
            {
                FacilityCode = "CR-101",
                FacilityName = "Phòng học lý thuyết 101 (Classroom 101)",
                FacilityType = FacilityType.Classroom,
                Capacity = 35,
                IsActive = true,
                Description = "[Mẫu/Sample] Phòng học lý thuyết trang bị máy chiếu và điều hòa tiêu chuẩn ICAO.",
                LocationDetail = "Tầng 1 - Khu giảng đường A"
            },
            new TrainingFacility
            {
                FacilityCode = "CR-102",
                FacilityName = "Phòng học lý thuyết 102 (Classroom 102)",
                FacilityType = FacilityType.Classroom,
                Capacity = 30,
                IsActive = true,
                Description = "[Mẫu/Sample] Phòng học lý thuyết trang bị bảng tương tác.",
                LocationDetail = "Tầng 1 - Khu giảng đường A"
            },
            new TrainingFacility
            {
                FacilityCode = "CR-103",
                FacilityName = "Phòng học Đa phương tiện 103 (Multimedia Smart Classroom 103)",
                FacilityType = FacilityType.Classroom,
                Capacity = 40,
                IsActive = true,
                Description = "[Mẫu/Sample] Phòng học trang bị máy tính trạm CBT, hệ thống âm thanh vòm và mô phỏng điện tử.",
                LocationDetail = "Tầng 2 - Khu giảng đường A"
            },
            new TrainingFacility
            {
                FacilityCode = "CR-201",
                FacilityName = "Phòng Điều độ & Khí tượng hàng không (Flight Dispatch & Met Lab 201)",
                FacilityType = FacilityType.Classroom,
                Capacity = 30,
                IsActive = true,
                Description = "[Mẫu/Sample] Trạm tra cứu thời tiết SIGMET/WAFS, tính toán hiệu năng cất hạ cánh và lập kế hoạch bay OFP.",
                LocationDetail = "Tầng 2 - Khu giảng đường B"
            },
            new TrainingFacility
            {
                FacilityCode = "CR-202",
                FacilityName = "Phòng Pháp quy & Hệ thống An toàn SMS (Aviation Law & SMS Center 202)",
                FacilityType = FacilityType.Classroom,
                Capacity = 35,
                IsActive = true,
                Description = "[Mẫu/Sample] Đào tạo Quy chế hàng không CAAV VAR Part 6, 8, 9, 10 và quản trị văn hóa an toàn Just Culture.",
                LocationDetail = "Tầng 2 - Khu giảng đường B"
            },
            new TrainingFacility
            {
                FacilityCode = "CR-301",
                FacilityName = "Hội trường Đào tạo Hàng không Quốc tế (Aviation Grand Auditorium 301)",
                FacilityType = FacilityType.Classroom,
                Capacity = 120,
                IsActive = true,
                Description = "[Mẫu/Sample] Hội trường lớn tổ chức hội thảo an toàn, khai giảng và tốt nghiệp phi công & kỹ sư.",
                LocationDetail = "Tầng 3 - Tòa nhà Điều hành Trung tâm"
            },

            // 2. Xưởng thực hành bảo dưỡng & cứu nguy cabin (Workshops)
            new TrainingFacility
            {
                FacilityCode = "WS-MAINT",
                FacilityName = "Xưởng bảo dưỡng kỹ thuật tàu bay (Hangar Workshop A)",
                FacilityType = FacilityType.Workshop,
                Capacity = 25,
                IsActive = true,
                Description = "[Mẫu/Sample] Xưởng thực hành động cơ, hệ thống cơ khí và điện tử tàu bay.",
                LocationDetail = "Hangar Bảo dưỡng số 1"
            },
            new TrainingFacility
            {
                FacilityCode = "WS-CABIN",
                FacilityName = "Khu huấn luyện an toàn cabin (Cabin Mockup Safety Center)",
                FacilityType = FacilityType.Workshop,
                Capacity = 30,
                IsActive = true,
                Description = "[Mẫu/Sample] Mô hình thân máy bay thực hành thoát hiểm, khói lửa và cứu sinh.",
                LocationDetail = "Tòa nhà Huấn luyện An toàn & Khẩn nguy"
            },
            new TrainingFacility
            {
                FacilityCode = "WS-AVIONIC",
                FacilityName = "Xưởng thực hành Điện tử & Khí tài tàu bay (Avionics & Radar Lab B)",
                FacilityType = FacilityType.Workshop,
                Capacity = 25,
                IsActive = true,
                Description = "[Mẫu/Sample] Bàn thực hành kiểm tra hệ thống Fly-by-wire, radar thời tiết, đài dẫn đường VOR/ILS và đài vô tuyến.",
                LocationDetail = "Hangar Kỹ thuật số 2"
            },
            new TrainingFacility
            {
                FacilityCode = "WS-ENG-JET",
                FacilityName = "Xưởng bảo dưỡng Động cơ phản lực (Jet Engine Maintenance Workshop)",
                FacilityType = FacilityType.Workshop,
                Capacity = 20,
                IsActive = true,
                Description = "[Mẫu/Sample] Thực hành tháo lắp, kiểm tra nội soi borescope động cơ phản lực CFM56 và LEAP-1A.",
                LocationDetail = "Hangar Bảo dưỡng số 3"
            },
            new TrainingFacility
            {
                FacilityCode = "WS-COMPOSITE",
                FacilityName = "Xưởng vật liệu Composite & Cấu trúc thân vỏ (Sheet Metal & Composite Lab)",
                FacilityType = FacilityType.Workshop,
                Capacity = 25,
                IsActive = true,
                Description = "[Mẫu/Sample] Thực hành gia công tán đinh hợp kim nhôm, sửa chữa kết cấu sợi carbon theo hướng dẫn SRM.",
                LocationDetail = "Hangar Kỹ thuật số 2"
            },
            new TrainingFacility
            {
                FacilityCode = "WS-EVAC-POOL",
                FacilityName = "Bể huấn luyện sơ tán tiếp nước & Sinh tồn biển (Wet Drill Evacuation Facility)",
                FacilityType = FacilityType.Workshop,
                Capacity = 45,
                IsActive = true,
                Description = "[Mẫu/Sample] Bể bơi tạo sóng chuyên dụng thực hành mở bè cứu sinh, sơ tán tiếp nước Ditching và áo phao.",
                LocationDetail = "Trung tâm Huấn luyện Cứu sinh & Sinh tồn biển"
            },
            new TrainingFacility
            {
                FacilityCode = "WS-DOOR-TRAINER",
                FacilityName = "Phòng huấn luyện Cửa thoát hiểm tàu bay (Door & Exit Trainer)",
                FacilityType = FacilityType.Workshop,
                Capacity = 25,
                IsActive = true,
                Description = "[Mẫu/Sample] Thiết bị mô phỏng đóng/mở cửa thoát hiểm A320/B737 có trợ lực khí nén và kẹt cửa khẩn cấp.",
                LocationDetail = "Tòa nhà Khẩn nguy Cabin - Tầng 2"
            },

            // 3. Buồng lái mô phỏng (Simulators - FSTD / FNPT / FFS)
            new TrainingFacility
            {
                FacilityCode = "SIM-A320",
                FacilityName = "Phòng mô phỏng A320 Full Flight Simulator (FFS-01)",
                FacilityType = FacilityType.Simulator,
                Capacity = 4,
                IsActive = true,
                Description = "[Mẫu/Sample] Thiết bị buồng lái mô phỏng A320 mức Level D chuẩn CAAV/EASA.",
                LocationDetail = "Tòa nhà Trung tâm Mô phỏng FSTD - Tầng trệt"
            },
            new TrainingFacility
            {
                FacilityCode = "SIM-DA42",
                FacilityName = "Phòng mô phỏng Diamond DA42 (FNPT II - 02)",
                FacilityType = FacilityType.Simulator,
                Capacity = 4,
                IsActive = true,
                Description = "[Mẫu/Sample] Thiết bị FNPT II phục vụ huấn luyện IFR và đa động cơ ME.",
                LocationDetail = "Tòa nhà Trung tâm Mô phỏng FSTD - Phòng 204"
            },
            new TrainingFacility
            {
                FacilityCode = "SIM-B737",
                FacilityName = "Phòng mô phỏng Boeing 737 Next Gen Full Flight Simulator (FFS-02)",
                FacilityType = FacilityType.Simulator,
                Capacity = 4,
                IsActive = true,
                Description = "[Mẫu/Sample] Thiết bị buồng lái mô phỏng B737-800 Level D chuẩn CAAV/FAA với hệ thống chuyển động 6 trục.",
                LocationDetail = "Tòa nhà Trung tâm Mô phỏng FSTD - Tầng trệt"
            },
            new TrainingFacility
            {
                FacilityCode = "SIM-A350",
                FacilityName = "Phòng mô phỏng Airbus A350 XWB Full Flight Simulator (FFS-03)",
                FacilityType = FacilityType.Simulator,
                Capacity = 4,
                IsActive = true,
                Description = "[Mẫu/Sample] Thiết bị buồng lái mô phỏng thân rộng A350 huấn luyện bay đường dài quốc tế ETOPS.",
                LocationDetail = "Tòa nhà Trung tâm Mô phỏng FSTD - Tầng 1"
            },
            new TrainingFacility
            {
                FacilityCode = "SIM-C172",
                FacilityName = "Phòng mô phỏng Cessna 172SP Garmin G1000 (FNPT I - 01)",
                FacilityType = FacilityType.Simulator,
                Capacity = 4,
                IsActive = true,
                Description = "[Mẫu/Sample] Thiết bị FNPT I kính bay hiện đại phục vụ giai đoạn đào tạo cơ bản PPL và bay khí tài.",
                LocationDetail = "Tòa nhà Trung tâm Mô phỏng FSTD - Phòng 201"
            },
            new TrainingFacility
            {
                FacilityCode = "SIM-HELI",
                FacilityName = "Phòng mô phỏng Trực thăng Đa năng (Helicopter FTD Level 3)",
                FacilityType = FacilityType.Simulator,
                Capacity = 4,
                IsActive = true,
                Description = "[Mẫu/Sample] Thiết bị FTD huấn luyện phi công trực thăng dân dụng, bay đêm và cất hạ cánh giàn khoan biển.",
                LocationDetail = "Khu Huấn luyện Cánh quay - Tòa nhà Mô phỏng"
            },

            // 4. Sân bay huấn luyện / Căn cứ bay (Airfields)
            new TrainingFacility
            {
                FacilityCode = "AIRPORT-VVPQ",
                FacilityName = "Căn cứ huấn luyện bay Phú Quốc (PQC Flight Base)",
                FacilityType = FacilityType.Airfield,
                Capacity = 100,
                IsActive = true,
                Description = "[Mẫu/Sample] Sân bay căn cứ huấn luyện thực hành bay VFR/IFR.",
                LocationDetail = "Cảng Hàng không Quốc tế Phú Quốc"
            },
            new TrainingFacility
            {
                FacilityCode = "AIRPORT-VVBM",
                FacilityName = "Căn cứ huấn luyện bay Buôn Ma Thuột (BMV Airfield Base)",
                FacilityType = FacilityType.Airfield,
                Capacity = 100,
                IsActive = true,
                Description = "[Mẫu/Sample] Sân bay huấn luyện thực hành bay đường dài Cross-Country.",
                LocationDetail = "Cảng Hàng không Buôn Ma Thuột"
            },
            new TrainingFacility
            {
                FacilityCode = "AIRPORT-VVCR",
                FacilityName = "Căn cứ huấn luyện bay Cam Ranh (CXR Flight Operations Base)",
                FacilityType = FacilityType.Airfield,
                Capacity = 120,
                IsActive = true,
                Description = "[Mẫu/Sample] Căn cứ huấn luyện cất hạ cánh ven biển, bay đường dài CPL và bay đêm tích lũy giờ bay.",
                LocationDetail = "Cảng Hàng không Quốc tế Cam Ranh"
            },
            new TrainingFacility
            {
                FacilityCode = "AIRPORT-VVCT",
                FacilityName = "Căn cứ huấn luyện bay Cần Thơ (VCA Flight Operations Base)",
                FacilityType = FacilityType.Airfield,
                Capacity = 80,
                IsActive = true,
                Description = "[Mẫu/Sample] Căn cứ thực hành tiếp cận thiết bị RNP/ILS vùng đồng bằng và điều kiện thời tiết gió mùa.",
                LocationDetail = "Cảng Hàng không Quốc tế Cần Thơ"
            },
            new TrainingFacility
            {
                FacilityCode = "AIRPORT-VVTS",
                FacilityName = "Căn cứ điều hành & Line Training Tân Sơn Nhất (SGN Flight Base)",
                FacilityType = FacilityType.Airfield,
                Capacity = 150,
                IsActive = true,
                Description = "[Mẫu/Sample] Căn cứ điều phối, huấn luyện đường dài thương mại và chuyển tiếp thực tế.",
                LocationDetail = "Cảng Hàng không Quốc tế Tân Sơn Nhất"
            },
            new TrainingFacility
            {
                FacilityCode = "AIRPORT-VVDN",
                FacilityName = "Căn cứ huấn luyện bay Đà Nẵng (DAD Flight Operations Base)",
                FacilityType = FacilityType.Airfield,
                Capacity = 100,
                IsActive = true,
                Description = "[Mẫu/Sample] Căn cứ thực hành bay tiếp cận địa hình đồi núi và thời tiết phức tạp khu vực miền Trung.",
                LocationDetail = "Cảng Hàng không Quốc tế Đà Nẵng"
            }
        };

        var toAdd = allFacilities.Where(f => !existingSet.Contains(f.FacilityCode)).ToList();
        if (toAdd.Count > 0)
        {
            context.TrainingFacilities.AddRange(toAdd);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedClassSchedulingAsync(AppDbContext context)
    {
        if (!await context.Classes.AnyAsync())
        {
            var courses = await context.Courses.ToDictionaryAsync(c => c.CourseCode, c => c.CourseId);
            context.Classes.Add(new Class { ClassCode = "AMT-101-C1", ClassName = "Aircraft Maintenance Technician Batch 1", CourseId = courses["AMT-101"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Completed });
            context.Classes.Add(new Class { ClassCode = "AMT-101-C2", ClassName = "Aircraft Maintenance Technician Batch 2", CourseId = courses["AMT-101"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Scheduled });
            context.Classes.Add(new Class { ClassCode = "AMT-101-C3", ClassName = "Aircraft Maintenance Technician Batch 3", CourseId = courses["AMT-101"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Planned });
            context.Classes.Add(new Class { ClassCode = "B737-TR-C1", ClassName = "B737 Type Rating Batch 1", CourseId = courses["B737-TR"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Completed });
            context.Classes.Add(new Class { ClassCode = "B737-TR-C2", ClassName = "B737 Type Rating Batch 2", CourseId = courses["B737-TR"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Scheduled });
            context.Classes.Add(new Class { ClassCode = "B737-TR-C3", ClassName = "B737 Type Rating Batch 3", CourseId = courses["B737-TR"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Planned });
            context.Classes.Add(new Class { ClassCode = "A320-FAM-C1", ClassName = "A320 Familiarization Batch 1", CourseId = courses["A320-FAM"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Completed });
            context.Classes.Add(new Class { ClassCode = "A320-FAM-C2", ClassName = "A320 Familiarization Batch 2", CourseId = courses["A320-FAM"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Scheduled });
            context.Classes.Add(new Class { ClassCode = "A320-FAM-C3", ClassName = "A320 Familiarization Batch 3", CourseId = courses["A320-FAM"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Planned });
            context.Classes.Add(new Class { ClassCode = "ENG-101-C1", ClassName = "Aviation English Batch 1", CourseId = courses["ENG-101"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Completed });
            context.Classes.Add(new Class { ClassCode = "ENG-101-C2", ClassName = "Aviation English Batch 2", CourseId = courses["ENG-101"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Scheduled });
            context.Classes.Add(new Class { ClassCode = "ENG-101-C3", ClassName = "Aviation English Batch 3", CourseId = courses["ENG-101"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Planned });
            context.Classes.Add(new Class { ClassCode = "SMS-101-C1", ClassName = "Safety Management Systems Batch 1", CourseId = courses["SMS-101"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Completed });
            context.Classes.Add(new Class { ClassCode = "SMS-101-C2", ClassName = "Safety Management Systems Batch 2", CourseId = courses["SMS-101"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Scheduled });
            context.Classes.Add(new Class { ClassCode = "SMS-101-C3", ClassName = "Safety Management Systems Batch 3", CourseId = courses["SMS-101"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Planned });
            context.Classes.Add(new Class { ClassCode = "HF-101-C1", ClassName = "Human Factors Batch 1", CourseId = courses["HF-101"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Completed });
            context.Classes.Add(new Class { ClassCode = "HF-101-C2", ClassName = "Human Factors Batch 2", CourseId = courses["HF-101"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Scheduled });
            context.Classes.Add(new Class { ClassCode = "HF-101-C3", ClassName = "Human Factors Batch 3", CourseId = courses["HF-101"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Planned });
            context.Classes.Add(new Class { ClassCode = "A350-TR-C1", ClassName = "A350 Type Rating Batch 1", CourseId = courses["A350-TR"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Completed });
            context.Classes.Add(new Class { ClassCode = "A350-TR-C2", ClassName = "A350 Type Rating Batch 2", CourseId = courses["A350-TR"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Scheduled });
            context.Classes.Add(new Class { ClassCode = "A350-TR-C3", ClassName = "A350 Type Rating Batch 3", CourseId = courses["A350-TR"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Planned });
            context.Classes.Add(new Class { ClassCode = "B787-TR-C1", ClassName = "B787 Type Rating Batch 1", CourseId = courses["B787-TR"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Completed });
            context.Classes.Add(new Class { ClassCode = "B787-TR-C2", ClassName = "B787 Type Rating Batch 2", CourseId = courses["B787-TR"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Scheduled });
            context.Classes.Add(new Class { ClassCode = "B787-TR-C3", ClassName = "B787 Type Rating Batch 3", CourseId = courses["B787-TR"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Planned });
            context.Classes.Add(new Class { ClassCode = "DGR-101-C1", ClassName = "Dangerous Goods Regulations Batch 1", CourseId = courses["DGR-101"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Completed });
            context.Classes.Add(new Class { ClassCode = "DGR-101-C2", ClassName = "Dangerous Goods Regulations Batch 2", CourseId = courses["DGR-101"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Scheduled });
            context.Classes.Add(new Class { ClassCode = "DGR-101-C3", ClassName = "Dangerous Goods Regulations Batch 3", CourseId = courses["DGR-101"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Planned });
            context.Classes.Add(new Class { ClassCode = "SEC-101-C1", ClassName = "Aviation Security Batch 1", CourseId = courses["SEC-101"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Completed });
            context.Classes.Add(new Class { ClassCode = "SEC-101-C2", ClassName = "Aviation Security Batch 2", CourseId = courses["SEC-101"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Scheduled });
            context.Classes.Add(new Class { ClassCode = "SEC-101-C3", ClassName = "Aviation Security Batch 3", CourseId = courses["SEC-101"], StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(1), Location = "Hangar", Capacity = 30, Status = ClassStatus.Planned });
            await context.SaveChangesAsync();
        }

        if (!await context.ClassSubjects.AnyAsync())
        {
            var instructors = await context.Accounts.Where(a => a.RoleId == 2).Select(a => a.AccountId).ToListAsync(); // 2 is instructor usually, but let's just fetch dynamically
            var instructorIds = await context.Accounts.Where(a => a.Username.StartsWith("instructor")).Select(a => a.AccountId).ToListAsync();
            var classEntities = await context.Classes.ToListAsync();
            var courseSubjects = await context.CourseSubjects.ToListAsync();

            var rand = new Random(42);
            int instructorIndex = 0;
            foreach(var cls in classEntities)
            {
                var subjectsForCourse = courseSubjects.Where(cs => cs.CourseId == cls.CourseId).ToList();
                foreach(var sub in subjectsForCourse)
                {
                    var instructorId = instructorIds[instructorIndex++ % instructorIds.Count];
                    context.ClassSubjects.Add(new ClassSubject { ClassId = cls.ClassId, SubjectId = sub.SubjectId, InstructorAccountId = instructorId, CreatedAt = DateTime.UtcNow });
                }
            }
            await context.SaveChangesAsync();
        }

        if (!await context.Sessions.AnyAsync())
        {
            var classSubjects = await context.ClassSubjects.ToListAsync();
            var assessments = await context.Assessments.ToListAsync();
            var checklists = await context.PracticalChecklists.ToListAsync();
            var rand = new Random(42);

            foreach(var cs in classSubjects)
            {
                var assessment = assessments.FirstOrDefault(a => a.SubjectId == cs.SubjectId);
                var checklist = checklists.FirstOrDefault(c => c.SubjectId == cs.SubjectId);

                var baseDate = DateTime.UtcNow.Date.AddDays(rand.Next(-14, 14)).AddHours(rand.Next(8, 17));
                var s1Date = baseDate;
                var s2Date = baseDate.AddDays(2).Date.AddHours(rand.Next(8, 17));

                // Session 1 is confirmed, Session 2 is unconfirmed so instructor can test attendance
                context.Sessions.Add(new Session { ClassId = cs.ClassId, SubjectId = cs.SubjectId, SessionTitle = "Session 1", SessionDate = s1Date, StartAt = s1Date, EndAt = s1Date.AddHours(2), IsConfirmed = true, ConfirmedByAccountId = cs.InstructorAccountId });
                context.Sessions.Add(new Session { ClassId = cs.ClassId, SubjectId = cs.SubjectId, SessionTitle = "Session 2", SessionDate = s2Date, StartAt = s2Date, EndAt = s2Date.AddHours(2), IsConfirmed = false, ConfirmedByAccountId = null });

                // Add exam sessions
                if (assessment != null)
                {
                    var examDate = baseDate.AddDays(4).Date.AddHours(rand.Next(8, 17));
                    context.Sessions.Add(new Session { ClassId = cs.ClassId, SubjectId = cs.SubjectId, SessionTitle = "Theory Exam", SessionDate = examDate, StartAt = examDate, EndAt = examDate.AddHours(2), IsConfirmed = false, ConfirmedByAccountId = null, IsAssessmentRequired = true, AssessmentId = assessment.AssessmentId });
                }

                if (checklist != null)
                {
                    var pracDate = baseDate.AddDays(5).Date.AddHours(rand.Next(8, 17));
                    context.Sessions.Add(new Session { ClassId = cs.ClassId, SubjectId = cs.SubjectId, SessionTitle = "Practical Exam", SessionDate = pracDate, StartAt = pracDate, EndAt = pracDate.AddHours(2), IsConfirmed = false, ConfirmedByAccountId = null, IsChecklistRequired = true, PracticalChecklistId = checklist.PracticalChecklistId });
                }
            }
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedEnrollmentAsync(AppDbContext context)
    {
        if (!await context.CourseEnrollments.AnyAsync())
        {
            var studentIds = await context.Accounts.Where(a => a.Username.StartsWith("student")).Select(a => a.AccountId).ToListAsync();
            var classEntities = await context.Classes.ToListAsync();
            var rand = new Random(42);

            foreach (var stu in studentIds)
            {
                // Each student enrolls in 3 random classes
                var selectedClasses = classEntities.OrderBy(x => rand.Next()).Take(3).ToList();
                foreach (var cls in selectedClasses)
                {
                    context.CourseEnrollments.Add(new CourseEnrollment
                    {
                        AccountId = stu,
                        ClassId = cls.ClassId,
                        Status = cls.Status == ClassStatus.Completed ? EnrollmentStatus.Completed : EnrollmentStatus.Enrolled,
                        EnrolledAt = DateTime.UtcNow.AddMonths(-1)
                    });
                }
            }
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedEtrAndSubjectResultsAsync(AppDbContext context)
    {
        if (!await context.ETRCourseRecords.AnyAsync())
        {
            var enrollments = await context.CourseEnrollments.ToListAsync();
            var classes = await context.Classes.ToDictionaryAsync(c => c.ClassId, c => c.CourseId);
            var courses = await context.Courses.ToDictionaryAsync(c => c.CourseId, c => c);
            var rand = new Random(42);
            int certIndex = 0;

            foreach(var enrollment in enrollments)
            {
                var status = enrollment.Status == EnrollmentStatus.Completed ? EtrStatus.Completed : EtrStatus.InProgress;
                var etr = new ETRCourseRecord
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    CourseVersionNo = 1,
                    Status = status
                };

                if (status != EtrStatus.Draft)
                {
                    etr.SubmittedAt = DateTime.UtcNow.AddDays(-10);
                }
                if (status == EtrStatus.Completed || status == EtrStatus.Verified)
                {
                    etr.VerifiedAt = DateTime.UtcNow.AddDays(-2);
                    etr.CompletedAt = DateTime.UtcNow.AddDays(-2);
                    etr.IsLocked = true;

                    // Seed IssuedDate and ExpiryDate for Completed certificates
                    classes.TryGetValue(enrollment.ClassId, out var courseId);
                    if (courseId > 0 && courses.TryGetValue(courseId, out var course) && course.ValidityMonths.HasValue)
                    {
                        var validityMonths = course.ValidityMonths.Value;
                        certIndex++;
                        switch (certIndex % 5)
                        {
                            case 0:
                                // Expired: hết hạn 15 ngày trước
                                etr.ExpiryDate = DateTime.UtcNow.AddDays(-15);
                                etr.IssuedDate = etr.ExpiryDate.Value.AddMonths(-validityMonths);
                                break;
                            case 1:
                                // Expired: hết hạn 5 ngày trước
                                etr.ExpiryDate = DateTime.UtcNow.AddDays(-5);
                                etr.IssuedDate = etr.ExpiryDate.Value.AddMonths(-validityMonths);
                                break;
                            case 2:
                                // ExpiringSoon: còn 7 ngày (trong ngưỡng 30 ngày)
                                etr.ExpiryDate = DateTime.UtcNow.AddDays(7);
                                etr.IssuedDate = etr.ExpiryDate.Value.AddMonths(-validityMonths);
                                break;
                            case 3:
                                // ExpiringSoon: còn 20 ngày (trong ngưỡng 30 ngày)
                                etr.ExpiryDate = DateTime.UtcNow.AddDays(20);
                                etr.IssuedDate = etr.ExpiryDate.Value.AddMonths(-validityMonths);
                                break;
                            default:
                                // Valid: còn hạn dài (6 tháng)
                                etr.IssuedDate = DateTime.UtcNow.AddMonths(-2);
                                etr.ExpiryDate = etr.IssuedDate.Value.AddMonths(validityMonths);
                                break;
                        }
                    }
                }

                context.ETRCourseRecords.Add(etr);
            }
            await context.SaveChangesAsync();
        }

        if (!await context.SubjectResults.AnyAsync())
        {
            var etrs = await context.ETRCourseRecords.ToListAsync();
            var enrollments = await context.CourseEnrollments.ToListAsync();
            var classSubjects = await context.ClassSubjects.ToListAsync();

            foreach(var etr in etrs)
            {
                var enrollment = enrollments.FirstOrDefault(e => e.EnrollmentId == etr.EnrollmentId);
                if (enrollment == null) continue;

                var subjects = classSubjects.Where(cs => cs.ClassId == enrollment.ClassId).ToList();
                foreach(var sub in subjects)
                {
                    var cls = await context.Classes.FirstAsync(c => c.ClassId == enrollment.ClassId);
                    context.SubjectResults.Add(new SubjectResult
                    {
                        EtrId = etr.ETRCourseRecordId,
                        CourseId = cls.CourseId,
                        SubjectId = sub.SubjectId,
                        AttendanceRate = 100m,
                        Score = 85m,
                        Status = SubjectResultStatus.Passed,
                        EvaluatedByAccountId = sub.InstructorAccountId,
                        EvaluatedAt = DateTime.UtcNow.AddDays(-1)
                    });
                }
            }
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedAttendanceAsync(AppDbContext context)
    {
        if (!await context.AttendanceRecords.AnyAsync())
        {
            var sessions = await context.Sessions.ToListAsync();
            var enrollments = await context.CourseEnrollments.ToListAsync();
            var rand = new Random(42);

            foreach(var session in sessions)
            {
                if (!session.IsConfirmed) continue;

                var sessionEnrollments = enrollments.Where(e => e.ClassId == session.ClassId).ToList();
                foreach(var e in sessionEnrollments)
                {
                    context.AttendanceRecords.Add(new AttendanceRecord
                    {
                        SessionId = session.SessionId,
                        EnrollmentId = e.EnrollmentId,
                        Status = rand.NextDouble() > 0.1 ? AttendanceStatus.Present : AttendanceStatus.Absent,
                        RecordedByAccountId = session.ConfirmedByAccountId ?? 1,
                        RecordedAt = DateTime.UtcNow
                    });
                }
            }
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedAssessmentResultsAsync(AppDbContext context)
    {
        if (!await context.AssessmentResults.AnyAsync())
        {
            var subjectResults = await context.SubjectResults.ToListAsync();
            var assessments = await context.Assessments.ToListAsync();
            var rand = new Random(42);

            foreach(var sr in subjectResults)
            {
                var subAssessments = assessments.Where(a => a.CourseId == sr.CourseId && a.SubjectId == sr.SubjectId).ToList();
                foreach(var a in subAssessments)
                {
                    var etr = await context.ETRCourseRecords.FirstAsync(e => e.ETRCourseRecordId == sr.EtrId);
                    var enrollment = await context.CourseEnrollments.FirstAsync(e => e.EnrollmentId == etr.EnrollmentId);

                    context.AssessmentResults.Add(new AssessmentResult
                    {
                        AssessmentId = a.AssessmentId,
                        AccountId = enrollment.AccountId,
                        SubjectResultId = sr.SubjectResultId,
                        Score = rand.Next(70, 100),
                        ResultStatus = "Passed",
                        GradedByAccountId = sr.EvaluatedByAccountId ?? 1,
                        TakenAt = DateTime.UtcNow,
                        RecordedAt = DateTime.UtcNow,
                        IsPublished = true,
                        AttemptNo = 1
                    });
                }
            }
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedPracticalChecklistResultsAsync(AppDbContext context)
    {
        if (!await context.PracticalChecklistResults.AnyAsync())
        {
            var subjectResults = await context.SubjectResults.ToListAsync();
            var checklists = await context.PracticalChecklists.ToListAsync();
            var qaId = (await context.Accounts.FirstAsync(a => a.Username == QaUsername)).AccountId;

            foreach(var sr in subjectResults)
            {
                var subChecklists = checklists.Where(c => c.CourseId == sr.CourseId && c.SubjectId == sr.SubjectId).ToList();
                foreach(var c in subChecklists)
                {
                    context.PracticalChecklistResults.Add(new PracticalChecklistResult
                    {
                        SubjectResultId = sr.SubjectResultId,
                        PracticalChecklistId = c.PracticalChecklistId,
                        Score = 100m,
                        ResultStatus = "Completed",
                        VerifiedByAccountId = qaId,
                        CompletedAt = DateTime.UtcNow,
                        IsPublished = true
                    });
                }
            }
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedSignoffAsync(AppDbContext context)
    {
        if (!await context.SubjectSignoffs.AnyAsync())
        {
            var subjectResults = await context.SubjectResults.ToListAsync();
            foreach(var sr in subjectResults)
            {
                context.SubjectSignoffs.Add(new SubjectSignoff
                {
                    SubjectResultId = sr.SubjectResultId,
                    SignoffByAccountId = sr.EvaluatedByAccountId ?? 1,
                    Role = "Instructor",
                    SignoffAt = DateTime.UtcNow,
                    Comment = "Passed"
                });
            }
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedEvidenceAsync(AppDbContext context)
    {
        if (!await context.EvidenceFiles.AnyAsync())
        {
            var subjectResults = await context.SubjectResults.ToListAsync();
            var evidenceTypes = await context.EvidenceTypes.ToListAsync();
            var qaId = (await context.Accounts.FirstAsync(a => a.Username == QaUsername)).AccountId;
            var rand = new Random(42);

            // Evidence files no longer store bytes/paths on this server — the FE uploads straight to
            // Cloudinary and the backend only keeps the URL (see Attachment.cs). Seed data below
            // points at Cloudinary's own public sample assets so the dummy rows still resolve to a
            // real, viewable file.
            var sampleResults = subjectResults.OrderBy(x => rand.Next()).Take(50).ToList();

            foreach(var sr in sampleResults)
            {
                var etr = await context.ETRCourseRecords.FirstAsync(e => e.ETRCourseRecordId == sr.EtrId);
                var enrollment = await context.CourseEnrollments.FirstAsync(e => e.EnrollmentId == etr.EnrollmentId);

                var photoEvidence = new EvidenceFile
                {
                    EvidenceTypeId = evidenceTypes[0].EvidenceTypeId, // Photo
                    UploadedByAccountId = sr.EvaluatedByAccountId ?? 1,
                    AccountId = enrollment.AccountId,
                    SubjectResultId = sr.SubjectResultId,
                    VerificationStatus = "Verified",
                    VerifiedByAccountId = qaId,
                    VerifiedAt = DateTime.UtcNow,
                    UploadedAt = DateTime.UtcNow
                };
                context.EvidenceFiles.Add(photoEvidence);
                await context.SaveChangesAsync();
                context.Attachments.Add(new Attachment
                {
                    OwnerType = nameof(EvidenceFile),
                    OwnerId = photoEvidence.EvidenceFileId,
                    Url = "https://res.cloudinary.com/demo/image/upload/sample.jpg",
                    FileName = "dummy_evidence.jpg",
                    MimeType = "image/jpeg",
                    FileSize = 135,
                    UploadedByAccountId = photoEvidence.UploadedByAccountId,
                    UploadedAt = DateTime.UtcNow
                });

                var pdfEvidence = new EvidenceFile
                {
                    EvidenceTypeId = evidenceTypes[2].EvidenceTypeId, // PDF
                    UploadedByAccountId = sr.EvaluatedByAccountId ?? 1,
                    AccountId = enrollment.AccountId,
                    SubjectResultId = sr.SubjectResultId,
                    VerificationStatus = "Verified",
                    VerifiedByAccountId = qaId,
                    VerifiedAt = DateTime.UtcNow,
                    UploadedAt = DateTime.UtcNow
                };
                context.EvidenceFiles.Add(pdfEvidence);
                await context.SaveChangesAsync();
                context.Attachments.Add(new Attachment
                {
                    OwnerType = nameof(EvidenceFile),
                    OwnerId = pdfEvidence.EvidenceFileId,
                    Url = "https://res.cloudinary.com/demo/image/upload/sample.pdf",
                    FileName = "dummy_evidence.pdf",
                    MimeType = "application/pdf",
                    FileSize = 402,
                    UploadedByAccountId = pdfEvidence.UploadedByAccountId,
                    UploadedAt = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedApprovalWorkflowAsync(AppDbContext context)
    {
        var etrs = await context.ETRCourseRecords.ToListAsync();
        var managerId = (await context.Accounts.FirstAsync(a => a.Username == ManagerUsername)).AccountId;
        var qaId = (await context.Accounts.FirstAsync(a => a.Username == QaUsername)).AccountId;
        var instructorId = (await context.Accounts.FirstAsync(a => a.Username == InstructorUsername)).AccountId;

        // 1. Ensure all Completed ETRs have valid IssuedDate and ExpiryDate
        foreach (var etr in etrs)
        {
            if (etr.Status == EtrStatus.Completed || etr.IsLocked)
            {
                if (!etr.IssuedDate.HasValue)
                {
                    etr.IssuedDate = etr.CompletedAt ?? DateTime.UtcNow.AddMonths(-2);
                }
                if (!etr.ExpiryDate.HasValue)
                {
                    etr.ExpiryDate = etr.IssuedDate.Value.AddMonths(24);
                }
            }
        }
        await context.SaveChangesAsync();

        // 2. Ensure all ETRs have corresponding ApprovalRequest and ApprovalHistories
        var existingRequestEtrIds = (await context.ApprovalRequests.Select(r => r.ETRCourseRecordId).ToListAsync()).ToHashSet();

        foreach (var etr in etrs)
        {
            if (existingRequestEtrIds.Contains(etr.ETRCourseRecordId))
                continue;

            string status = etr.Status switch
            {
                EtrStatus.Completed => "Approved",
                EtrStatus.Verified => "UnderReview",
                EtrStatus.Submitted => "Pending",
                EtrStatus.ReturnedForCorrection => "Rejected",
                _ => "Pending"
            };

            var submitTime = etr.SubmittedAt ?? DateTime.UtcNow.AddDays(-10);
            var verifyTime = etr.VerifiedAt ?? submitTime.AddDays(3);
            var approveTime = etr.CompletedAt ?? verifyTime.AddDays(2);

            var request = new ApprovalRequest
            {
                ETRCourseRecordId = etr.ETRCourseRecordId,
                CurrentStatus = status,
                SubmittedByAccountId = instructorId,
                SubmittedAt = submitTime,
                CurrentApproverId = managerId,
                CompletedAt = (status == "Approved" || status == "Rejected") ? approveTime : null
            };
            context.ApprovalRequests.Add(request);
            await context.SaveChangesAsync(); // Save to obtain ApprovalRequestId

            // Stage 1: Academic Staff / Instructor Submit
            context.ApprovalHistories.Add(new ApprovalHistory
            {
                ApprovalRequestId = request.ApprovalRequestId,
                ActionByAccountId = instructorId,
                ActionType = ApprovalHistoryActionType.Submit.ToString(),
                PreviousStatus = "Draft",
                NewStatus = "Pending",
                Comments = "Hồ sơ ETR hoàn thiện và nộp lên QA thẩm định.",
                ActionAt = submitTime
            });

            // Stage 2: QA Review / Verification
            if (status == "UnderReview" || status == "Approved" || status == "Rejected" || etr.Status == EtrStatus.Verified || etr.Status == EtrStatus.Completed || etr.IsLocked)
            {
                context.ApprovalHistories.Add(new ApprovalHistory
                {
                    ApprovalRequestId = request.ApprovalRequestId,
                    ActionByAccountId = qaId,
                    ActionType = ApprovalHistoryActionType.Review.ToString(),
                    PreviousStatus = "Pending",
                    NewStatus = "UnderReview",
                    Comments = "Phòng QA đã kiểm tra đối chiếu dữ liệu bay, SIM và minh chứng.",
                    ActionAt = verifyTime
                });
            }

            // Stage 3: Training Manager Approval
            if (status == "Approved" || etr.Status == EtrStatus.Completed || etr.IsLocked)
            {
                context.ApprovalHistories.Add(new ApprovalHistory
                {
                    ApprovalRequestId = request.ApprovalRequestId,
                    ActionByAccountId = managerId,
                    ActionType = ApprovalHistoryActionType.Approve.ToString(),
                    PreviousStatus = "UnderReview",
                    NewStatus = "Approved",
                    Comments = "Trưởng phòng Quản lý Đào tạo phê duyệt hoàn thành hồ sơ huấn luyện ETR.",
                    ActionAt = approveTime
                });
            }
            else if (status == "Rejected")
            {
                context.ApprovalHistories.Add(new ApprovalHistory
                {
                    ApprovalRequestId = request.ApprovalRequestId,
                    ActionByAccountId = managerId,
                    ActionType = ApprovalHistoryActionType.Reject.ToString(),
                    PreviousStatus = "UnderReview",
                    NewStatus = "Rejected",
                    Comments = "Hồ sơ yêu cầu bổ sung minh chứng giờ bay.",
                    ActionAt = approveTime
                });
            }
        }
        await context.SaveChangesAsync();
    }

    private static async Task SeedMiscellaneousAsync(AppDbContext context)
    {
        if (!await context.ExportJobs.AnyAsync())
        {
            var adminId = (await context.Accounts.FirstAsync(a => a.Username == AdminUsername)).AccountId;

            context.ExportJobs.AddRange(
                new ExportJob { RequestedByAccountId = adminId, ExportType = "ComplianceReport", Status = ExportJobStatus.Completed, RequestedAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow, FileName = "report1.pdf", FilePath = "/exports/report1.pdf" },
                new ExportJob { RequestedByAccountId = adminId, ExportType = "TrainingPackage", Status = ExportJobStatus.InProgress, RequestedAt = DateTime.UtcNow },
                new ExportJob { RequestedByAccountId = adminId, ExportType = "TrainingPackage", Status = ExportJobStatus.Completed, RequestedAt = DateTime.UtcNow.AddDays(-1), CompletedAt = DateTime.UtcNow.AddDays(-1), FileName = "package1.zip", FilePath = "/exports/package1.zip" }
            );
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedAmendmentRequestsAsync(AppDbContext context)
    {
        if (!await context.AmendmentRequests.AnyAsync(a => a.Status == AmendmentStatus.Pending))
        {
            var sr = await context.SubjectResults.FirstOrDefaultAsync(s => s.Status == SubjectResultStatus.Passed)
                     ?? await context.SubjectResults.FirstOrDefaultAsync();
            if (sr != null)
            {
                var instructor = await context.Accounts.FirstOrDefaultAsync(a => a.Username == InstructorUsername);
                var instructorId = instructor?.AccountId ?? sr.EvaluatedByAccountId ?? 1;

                context.AmendmentRequests.Add(new AmendmentRequest
                {
                    SubjectResultId = sr.SubjectResultId,
                    RequestedByAccountId = instructorId,
                    Reason = "Instructor requested unlock to update practical assessment score following re-evaluation.",
                    OldValue = sr.Status.ToString(),
                    Status = AmendmentStatus.Pending,
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                });
                await context.SaveChangesAsync();
            }
        }
    }
}

