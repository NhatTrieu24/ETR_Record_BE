using ETR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ETR.Infrastructure.Data;

/// <summary>
/// Single source of truth for baseline + demo seed data. Runs on every app startup
/// (see Program.cs, after Database.MigrateAsync). Each module guards itself with an
/// "AnyAsync" check so it only inserts once per table — to change seed data, edit the
/// values below and reset the target database's data (see Deploy_NukeAndSeed.sql),
/// then restart the app.
///
/// Note: on databases migrated before 2026-07-23, Roles/Departments/Accounts may already
/// exist with plaintext PasswordHash values from the historical SeedSystemData migration's
/// raw-SQL insert — in that case the Identity module below is a no-op and those accounts
/// won't authenticate until the database's Accounts table is cleared and the app restarted.
/// The migration's raw-SQL seed was removed (it also used sp_MSForEachTable, which isn't
/// available on Azure SQL Database) — this seeder is now the only source for that data.
/// </summary>
public static class DataSeeder
{
    private const string AdminUsername = "admin@etr.com";
    private const string StudentUsername = "student@etr.com";
    private const string InstructorUsername = "instructor@etr.com";
    private const string QaUsername = "qa@etr.com";
    private const string ManagerUsername = "manager@etr.com";
    private const string ManagementViewerUsername = "management-viewer@etr.com";
    private const string CourseCode = "AMT-101";
    private const string ClassCode = "AMT101-C1";

    public static async Task SeedAsync(AppDbContext context)
    {
        await SeedIdentityAsync(context);
        await SeedCatalogAsync(context);
        await SeedClassSchedulingAsync(context);
        await SeedEnrollmentAsync(context);
        await SeedEtrAndSubjectResultsAsync(context);
        await SeedAttendanceAsync(context);
        await SeedAssessmentResultsAsync(context);
        await SeedPracticalChecklistResultsAsync(context);
        await SeedSignoffAsync(context);
        await SeedEvidenceAsync(context);
        await SeedApprovalWorkflowAsync(context);
        await SeedMiscellaneousAsync(context);
    }

    // ===================== Module: Identity =====================
    // Role, Department, Account, UserProfile

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
                new Role { RoleName = "ManagementViewer", Description = "Read-only leadership dashboard/report viewer" });
            await context.SaveChangesAsync();
        }
        else if (!await context.Roles.AnyAsync(r => r.RoleName == "ManagementViewer"))
        {
            // Added after the initial 7-role seed (FRD `DS_các_role_cuối_cùng_.txt`) — on a database
            // already seeded before this role existed, the AnyAsync() guard above is a no-op, so it
            // needs its own idempotent insert here (same pattern as the per-item Department loop below).
            context.Roles.Add(new Role { RoleName = "ManagementViewer", Description = "Read-only leadership dashboard/report viewer" });
            await context.SaveChangesAsync();
        }

        var defaultDepartments = new[]
        {
            new Department { DepartmentName = "Administration", Description = "General Admin" },
            new Department { DepartmentName = "Training", Description = "Training Dept" },
            new Department { DepartmentName = "Flight Crew", Description = "Flight Crew Dept" },
            new Department { DepartmentName = "Cabin Crew", Description = "Cabin Crew Dept" },
            new Department { DepartmentName = "Engineering & Maintenance", Description = "Engineering & Maintenance Dept" },
            new Department { DepartmentName = "Ground Operations", Description = "Ground Operations Dept" }
        };

        foreach (var dept in defaultDepartments)
        {
            if (!await context.Departments.AnyAsync(d => d.DepartmentName == dept.DepartmentName))
            {
                context.Departments.Add(dept);
            }
        }
        await context.SaveChangesAsync();

        if (!await context.Accounts.AnyAsync())
        {
            var roleIds = await context.Roles.ToDictionaryAsync(r => r.RoleName, r => r.RoleId);
            var deptIds = await context.Departments.ToDictionaryAsync(d => d.DepartmentName, d => d.DepartmentId);

            context.Accounts.AddRange(
                CreateAccount(AdminUsername, roleIds["Admin"], deptIds["Administration"], "ADM-01", "System Admin", new DateTime(1980, 1, 1), "Other"),
                CreateAccount(InstructorUsername, roleIds["Instructor"], deptIds["Training"], "INS-01", "Senior Instructor", new DateTime(1985, 1, 1), "Male"),
                CreateAccount(QaUsername, roleIds["QA"], deptIds["Administration"], "QA-01", "QA Specialist", new DateTime(1990, 1, 1), "Female"),
                CreateAccount("academic@etr.com", roleIds["Academic"], deptIds["Administration"], "ACA-01", "Academic Staff", new DateTime(1992, 1, 1), "Female"),
                CreateAccount(ManagerUsername, roleIds["TrainingManager"], deptIds["Training"], "MGR-01", "Training Manager", new DateTime(1988, 1, 1), "Male"),
                CreateAccount(StudentUsername, roleIds["Student"], deptIds["Training"], "STU-01", "Jane Student", new DateTime(2000, 1, 1), "Female"),
                CreateAccount("audit@etr.com", roleIds["Audit"], deptIds["Administration"], "AUD-01", "Audit Staff", new DateTime(1985, 1, 1), "Other"),
                CreateAccount(ManagementViewerUsername, roleIds["ManagementViewer"], deptIds["Administration"], "MGV-01", "Management Viewer", new DateTime(1978, 1, 1), "Other"));
            await context.SaveChangesAsync();
        }
        else if (!await context.Accounts.AnyAsync(a => a.Username == ManagementViewerUsername))
        {
            // Same "added after initial seed" situation as the ManagementViewer role above.
            var roleIds = await context.Roles.ToDictionaryAsync(r => r.RoleName, r => r.RoleId);
            var deptIds = await context.Departments.ToDictionaryAsync(d => d.DepartmentName, d => d.DepartmentId);
            context.Accounts.Add(CreateAccount(ManagementViewerUsername, roleIds["ManagementViewer"], deptIds["Administration"], "MGV-01", "Management Viewer", new DateTime(1978, 1, 1), "Other"));
            await context.SaveChangesAsync();
        }
    }

    private static Account CreateAccount(string username, int roleId, int departmentId, string userCode, string fullName, DateTime dateOfBirth, string gender)
    {
        return new Account
        {
            Username = username,
            // Demo credential remains "123456" for local/dev login convenience, but is
            // now stored as a bcrypt hash rather than plaintext.
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            RoleId = roleId,
            DepartmentId = departmentId,
            Status = "Active",
            Profile = new UserProfile
            {
                UserCode = userCode,
                FullName = fullName,
                Email = username,
                DateOfBirth = dateOfBirth,
                Gender = gender
            }
        };
    }

    // ===================== Module: Catalog / Curriculum =====================
    // Course, Subject, CourseSubject, CompletionRequirement, Assessment, PracticalChecklist, EvidenceType

    private static async Task SeedCatalogAsync(AppDbContext context)
    {
        if (!await context.Courses.AnyAsync())
        {
            context.Courses.AddRange(
                new Course
                {
                    CourseCode = CourseCode,
                    CourseName = "Aircraft Maintenance Technician - Basic",
                    Description = "Foundational course covering regulations, aircraft systems, practical maintenance skills, and safety for entry-level maintenance technicians.",
                    DurationHours = 120,
                    Status = "Active"
                },
                new Course { CourseCode = "B737-TR", CourseName = "B737 Type Rating", Description = "Type rating course for B737 NG/MAX.", DurationHours = 160, Status = "Active" },
                new Course { CourseCode = "A320-FAM", CourseName = "A320 Familiarization", Description = "A320 family general familiarization.", DurationHours = 40, Status = "Active" },
                new Course { CourseCode = "ENG-101", CourseName = "Aviation English", Description = "Aviation English for technicians.", DurationHours = 60, Status = "Active" }
            );
            await context.SaveChangesAsync();
        }

        if (!await context.Subjects.AnyAsync())
        {
            context.Subjects.AddRange(
                new Subject { SubjectCode = "SJ-REG", SubjectName = "Aviation Regulations & Compliance", SubjectType = "Theory", DefaultHours = 20, AssessmentMethod = "Written Exam", Description = "Civil aviation regulations and compliance requirements.", Status = "Active" },
                new Subject { SubjectCode = "SJ-SYS", SubjectName = "Aircraft Systems Fundamentals", SubjectType = "Theory", DefaultHours = 40, AssessmentMethod = "Written Exam", Description = "Core aircraft systems: hydraulics, electrical, avionics.", Status = "Active" },
                new Subject { SubjectCode = "SJ-PRA", SubjectName = "Practical Maintenance Skills", SubjectType = "Practical", DefaultHours = 50, AssessmentMethod = "Practical Checklist", Description = "Hands-on maintenance tasks performed under supervision.", Status = "Active" },
                new Subject { SubjectCode = "SJ-SAF", SubjectName = "Safety & Human Factors", SubjectType = "Theory", DefaultHours = 10, AssessmentMethod = "Written Exam", Description = "Human factors and safety management systems.", Status = "Active" });
            await context.SaveChangesAsync();
        }

        var course = await context.Courses.FirstAsync(c => c.CourseCode == CourseCode);
        var subjects = await context.Subjects.ToDictionaryAsync(s => s.SubjectCode, s => s);

        if (!await context.CourseSubjects.AnyAsync())
        {
            context.CourseSubjects.AddRange(
                new CourseSubject { CourseId = course.CourseId, SubjectId = subjects["SJ-REG"].SubjectId, SequenceNo = 1, RequiredHours = 20, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" },
                new CourseSubject { CourseId = course.CourseId, SubjectId = subjects["SJ-SYS"].SubjectId, SequenceNo = 2, RequiredHours = 40, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" },
                new CourseSubject { CourseId = course.CourseId, SubjectId = subjects["SJ-PRA"].SubjectId, SequenceNo = 3, RequiredHours = 50, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" },
                new CourseSubject { CourseId = course.CourseId, SubjectId = subjects["SJ-SAF"].SubjectId, SequenceNo = 4, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            await context.SaveChangesAsync();
        }

        if (!await context.CompletionRequirements.AnyAsync())
        {
            context.CompletionRequirements.AddRange(
                new CompletionRequirement { CourseId = course.CourseId, RequirementName = "Minimum 80% Attendance", IsMandatory = true, DisplayOrder = 1, RequirementType = "MinAttendance", ThresholdValue = 80m },
                new CompletionRequirement { CourseId = course.CourseId, RequirementName = "All Assessments Passed", Description = "Every mandatory assessment scored at or above its passing score.", IsMandatory = true, DisplayOrder = 2, RequirementType = "AllAssessmentsPassed" },
                new CompletionRequirement { CourseId = course.CourseId, RequirementName = "All Practical Checklists Signed Off", IsMandatory = true, DisplayOrder = 3, RequirementType = "AllChecklistsSignedOff" },
                new CompletionRequirement { CourseId = course.CourseId, RequirementName = "OJT Hours Logged", Description = "Complete minimum OJT hours.", IsMandatory = false, DisplayOrder = 4, RequirementType = "Custom", ThresholdValue = 100m });
            await context.SaveChangesAsync();
        }

        if (!await context.Assessments.AnyAsync())
        {
            context.Assessments.AddRange(
                new Assessment { CourseId = course.CourseId, SubjectId = subjects["SJ-REG"].SubjectId, ComponentName = "Regulations Written Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 },
                new Assessment { CourseId = course.CourseId, SubjectId = subjects["SJ-SYS"].SubjectId, ComponentName = "Systems Midterm Quiz", AssessmentType = "Theory", Weight = 40, PassingScore = 70, IsRequired = true, DisplayOrder = 1 },
                new Assessment { CourseId = course.CourseId, SubjectId = subjects["SJ-SYS"].SubjectId, ComponentName = "Systems Final Exam", AssessmentType = "Theory", Weight = 60, PassingScore = 70, IsRequired = true, DisplayOrder = 2 },
                new Assessment { CourseId = course.CourseId, SubjectId = subjects["SJ-PRA"].SubjectId, ComponentName = "Practical Skills Exam", AssessmentType = "Practical", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 },
                new Assessment { CourseId = course.CourseId, SubjectId = subjects["SJ-SAF"].SubjectId, ComponentName = "Safety & Human Factors Exam", AssessmentType = "Theory", Weight = 100, PassingScore = 70, IsRequired = true, DisplayOrder = 1 });
            await context.SaveChangesAsync();
        }

        if (!await context.PracticalChecklists.AnyAsync())
        {
            var practicalSubjectId = subjects["SJ-PRA"].SubjectId;
            context.PracticalChecklists.AddRange(
                new PracticalChecklist { CourseId = course.CourseId, SubjectId = practicalSubjectId, ItemName = "Torque Wrench Calibration Check", IsRequired = true, DisplayOrder = 1 },
                new PracticalChecklist { CourseId = course.CourseId, SubjectId = practicalSubjectId, ItemName = "Panel Removal & Installation", IsRequired = true, DisplayOrder = 2 },
                new PracticalChecklist { CourseId = course.CourseId, SubjectId = practicalSubjectId, ItemName = "Hydraulic System Inspection", IsRequired = true, DisplayOrder = 3 },
                new PracticalChecklist { CourseId = course.CourseId, SubjectId = practicalSubjectId, ItemName = "Tool Control Accountability", IsRequired = true, DisplayOrder = 4 });
            await context.SaveChangesAsync();
        }

        if (!await context.EvidenceTypes.AnyAsync())
        {
            context.EvidenceTypes.AddRange(
                new EvidenceType { TypeName = "Photo Evidence", Description = "Photographic proof of task completion." },
                new EvidenceType { TypeName = "Signed Paper Form", Description = "Scanned physical sign-off form." },
                new EvidenceType { TypeName = "Digital Certificate", Description = "System-issued completion certificate." },
                new EvidenceType { TypeName = "Video Recording", Description = "Video proof of a practical task." });
            await context.SaveChangesAsync();
        }
    }

    // ===================== Module: Class & Scheduling =====================
    // Class, Session

    private static async Task SeedClassSchedulingAsync(AppDbContext context)
    {
        var course = await context.Courses.FirstAsync(c => c.CourseCode == CourseCode);

        if (!await context.Classes.AnyAsync())
        {
            context.Classes.AddRange(
                new Class
                {
                    ClassCode = ClassCode,
                    ClassName = "AMT-101 Batch 1",
                    CourseId = course.CourseId,
                    StartDate = new DateTime(2026, 1, 5),
                    EndDate = new DateTime(2026, 4, 30),
                    Location = "Hangar 3 Training Center",
                    Capacity = 20,
                    Status = "Completed"
                },
                new Class { ClassCode = "AMT101-C2", ClassName = "AMT-101 Batch 2", CourseId = course.CourseId, StartDate = new DateTime(2026, 5, 5), EndDate = new DateTime(2026, 8, 30), Location = "Hangar 3 Training Center", Capacity = 20, Status = "Scheduled" },
                new Class { ClassCode = "AMT101-C3", ClassName = "AMT-101 Batch 3", CourseId = course.CourseId, StartDate = new DateTime(2026, 9, 5), EndDate = new DateTime(2026, 12, 30), Location = "Hangar 3 Training Center", Capacity = 20, Status = "Scheduled" },
                new Class { ClassCode = "AMT101-C4", ClassName = "AMT-101 Batch 4", CourseId = course.CourseId, StartDate = new DateTime(2027, 1, 5), EndDate = new DateTime(2027, 4, 30), Location = "Hangar 3 Training Center", Capacity = 20, Status = "Planned" }
            );
            await context.SaveChangesAsync();
        }

        if (!await context.ClassSubjects.AnyAsync())
        {
            var cls = await context.Classes.FirstAsync(c => c.ClassCode == ClassCode);
            var instructorId = (await context.Accounts.FirstAsync(a => a.Username == InstructorUsername)).AccountId;
            var subjectIds = await context.Subjects.ToDictionaryAsync(s => s.SubjectCode, s => s.SubjectId);

            context.ClassSubjects.AddRange(
                new ClassSubject { ClassId = cls.ClassId, SubjectId = subjectIds["SJ-REG"], InstructorAccountId = instructorId, CreatedAt = DateTime.UtcNow },
                new ClassSubject { ClassId = cls.ClassId, SubjectId = subjectIds["SJ-SYS"], InstructorAccountId = instructorId, CreatedAt = DateTime.UtcNow },
                new ClassSubject { ClassId = cls.ClassId, SubjectId = subjectIds["SJ-PRA"], InstructorAccountId = instructorId, CreatedAt = DateTime.UtcNow },
                new ClassSubject { ClassId = cls.ClassId, SubjectId = subjectIds["SJ-SAF"], InstructorAccountId = instructorId, CreatedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();
        }

        if (!await context.Sessions.AnyAsync())
        {
            var cls = await context.Classes.FirstAsync(c => c.ClassCode == ClassCode);
            var instructorId = (await context.Accounts.FirstAsync(a => a.Username == InstructorUsername)).AccountId;
            var subjectIds = await context.Subjects.ToDictionaryAsync(s => s.SubjectCode, s => s.SubjectId);

            context.Sessions.AddRange(
                new Session { ClassId = cls.ClassId, SubjectId = subjectIds["SJ-REG"], SessionTitle = "Regulations Overview", SessionDate = new DateTime(2026, 1, 6), Location = "Room A", IsConfirmed = true, ConfirmedByAccountId = instructorId, ConfirmedAt = new DateTime(2026, 1, 6) },
                new Session { ClassId = cls.ClassId, SubjectId = subjectIds["SJ-SYS"], SessionTitle = "Systems Fundamentals Lecture", SessionDate = new DateTime(2026, 1, 20), Location = "Room A", IsConfirmed = true, ConfirmedByAccountId = instructorId, ConfirmedAt = new DateTime(2026, 1, 20), IsAssessmentRequired = true },
                new Session { ClassId = cls.ClassId, SubjectId = subjectIds["SJ-PRA"], SessionTitle = "Hands-on Workshop 1", SessionDate = new DateTime(2026, 2, 10), Location = "Hangar 3", IsConfirmed = true, ConfirmedByAccountId = instructorId, ConfirmedAt = new DateTime(2026, 2, 10), IsChecklistRequired = true },
                new Session { ClassId = cls.ClassId, SubjectId = subjectIds["SJ-SAF"], SessionTitle = "Human Factors Workshop", SessionDate = new DateTime(2026, 3, 1), Location = "Room B", IsConfirmed = true, ConfirmedByAccountId = instructorId, ConfirmedAt = new DateTime(2026, 3, 1), IsAssessmentRequired = true });
            await context.SaveChangesAsync();
        }
    }

    // ===================== Module: Enrollment =====================
    // CourseEnrollment

    private static async Task SeedEnrollmentAsync(AppDbContext context)
    {
        var cls = await context.Classes.FirstAsync(c => c.ClassCode == ClassCode);
        var student = await context.Accounts.FirstAsync(a => a.Username == StudentUsername);

        if (!await context.CourseEnrollments.AnyAsync())
        {
            var cls2 = await context.Classes.FirstAsync(c => c.ClassCode == "AMT101-C2");
            var cls3 = await context.Classes.FirstAsync(c => c.ClassCode == "AMT101-C3");
            var cls4 = await context.Classes.FirstAsync(c => c.ClassCode == "AMT101-C4");
            
            context.CourseEnrollments.AddRange(
                new CourseEnrollment
                {
                    AccountId = student.AccountId,
                    ClassId = cls.ClassId,
                    Status = "Completed",
                    EnrolledAt = new DateTime(2026, 1, 5),
                    StartDate = new DateTime(2026, 1, 5),
                    ExpectedCompletionDate = new DateTime(2026, 4, 30),
                    ActualCompletionDate = new DateTime(2026, 4, 30)
                },
                new CourseEnrollment { AccountId = student.AccountId, ClassId = cls2.ClassId, Status = "Enrolled", EnrolledAt = new DateTime(2026, 5, 1), ExpectedCompletionDate = new DateTime(2026, 8, 30) },
                new CourseEnrollment { AccountId = student.AccountId, ClassId = cls3.ClassId, Status = "Enrolled", EnrolledAt = new DateTime(2026, 9, 1), ExpectedCompletionDate = new DateTime(2026, 12, 30) },
                new CourseEnrollment { AccountId = student.AccountId, ClassId = cls4.ClassId, Status = "Withdrawn", EnrolledAt = new DateTime(2027, 1, 1), ExpectedCompletionDate = new DateTime(2027, 4, 30) }
            );
            await context.SaveChangesAsync();
        }
    }

    // ===================== Module: ETR & Subject Results =====================
    // ETRCourseRecord, SubjectResult

    private static async Task SeedEtrAndSubjectResultsAsync(AppDbContext context)
    {
        var course = await context.Courses.FirstAsync(c => c.CourseCode == CourseCode);
        var cls = await context.Classes.FirstAsync(c => c.ClassCode == ClassCode);
        var student = await context.Accounts.FirstAsync(a => a.Username == StudentUsername);
        var enrollment = await context.CourseEnrollments.FirstAsync(e => e.AccountId == student.AccountId && e.ClassId == cls.ClassId);

        if (!await context.ETRCourseRecords.AnyAsync())
        {
            var enrollments = await context.CourseEnrollments.Where(e => e.AccountId == student.AccountId).ToListAsync();
            var etrRecords = enrollments.Select(e => new ETRCourseRecord
            {
                EnrollmentId = e.EnrollmentId,
                Status = "InProgress",
                IsLocked = false,
                CreatedBySystem = true
            }).ToList();
            context.ETRCourseRecords.AddRange(etrRecords);
            await context.SaveChangesAsync();
        }

        if (!await context.SubjectResults.AnyAsync())
        {
            var etr = await context.ETRCourseRecords.FirstAsync(e => e.EnrollmentId == enrollment.EnrollmentId);
            var instructorId = (await context.Accounts.FirstAsync(a => a.Username == InstructorUsername)).AccountId;
            var courseSubjects = await context.CourseSubjects.Where(cs => cs.CourseId == course.CourseId).ToListAsync();

            var subjectResults = courseSubjects.Select(cs => new SubjectResult
            {
                EtrId = etr.ETRCourseRecordId,
                CourseId = cs.CourseId,
                SubjectId = cs.SubjectId,
                AttendanceRate = 100m,
                Score = 85m,
                Status = "Passed",
                EvaluatedByAccountId = instructorId,
                EvaluatedAt = new DateTime(2026, 4, 30)
            }).ToList();

            context.SubjectResults.AddRange(subjectResults);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>Shared lookups reused by the result-recording modules below.</summary>
    private static async Task<(Account Student, ETRCourseRecord Etr, CourseEnrollment Enrollment, Dictionary<string, SubjectResult> SubjectResultsByCode)> GetDemoContextAsync(AppDbContext context)
    {
        var student = await context.Accounts.FirstAsync(a => a.Username == StudentUsername);
        var cls = await context.Classes.FirstAsync(c => c.ClassCode == ClassCode);
        var enrollment = await context.CourseEnrollments.FirstAsync(e => e.AccountId == student.AccountId && e.ClassId == cls.ClassId);
        var etr = await context.ETRCourseRecords.FirstAsync(e => e.EnrollmentId == enrollment.EnrollmentId);

        var subjectCodesById = await context.Subjects.ToDictionaryAsync(s => s.SubjectId, s => s.SubjectCode);
        var subjectResultsByCode = await context.SubjectResults
            .Where(sr => sr.EtrId == etr.ETRCourseRecordId)
            .ToDictionaryAsync(sr => subjectCodesById[sr.SubjectId], sr => sr);

        return (student, etr, enrollment, subjectResultsByCode);
    }

    // ===================== Module: Attendance =====================
    // AttendanceRecord

    private static async Task SeedAttendanceAsync(AppDbContext context)
    {
        if (await context.AttendanceRecords.AnyAsync())
        {
            return;
        }

        var (_, _, enrollment, _) = await GetDemoContextAsync(context);
        var instructorId = (await context.Accounts.FirstAsync(a => a.Username == InstructorUsername)).AccountId;
        var sessions = await context.Sessions.Where(s => s.IsConfirmed).ToListAsync();

        var records = sessions.Select(s => new AttendanceRecord
        {
            SessionId = s.SessionId,
            EnrollmentId = enrollment.EnrollmentId,
            Status = "Present",
            RecordedByAccountId = instructorId,
            RecordedAt = s.SessionDate ?? DateTime.UtcNow
        }).ToList();

        context.AttendanceRecords.AddRange(records);
        await context.SaveChangesAsync();
    }

    // ===================== Module: Assessment Results & Retakes =====================
    // AssessmentResult, RetakeHistory

    private static async Task SeedAssessmentResultsAsync(AppDbContext context)
    {
        if (await context.AssessmentResults.AnyAsync())
        {
            return;
        }

        var (student, _, _, subjectResultsByCode) = await GetDemoContextAsync(context);
        var instructorId = (await context.Accounts.FirstAsync(a => a.Username == InstructorUsername)).AccountId;
        var assessments = await context.Assessments.ToDictionaryAsync(a => a.ComponentName, a => a);

        context.AssessmentResults.AddRange(
            BuildAssessmentResult(assessments["Regulations Written Exam"], student.AccountId, subjectResultsByCode["SJ-REG"].SubjectResultId, 88m, instructorId, new DateTime(2026, 1, 15)),
            BuildAssessmentResult(assessments["Systems Midterm Quiz"], student.AccountId, subjectResultsByCode["SJ-SYS"].SubjectResultId, 75m, instructorId, new DateTime(2026, 1, 25)),
            BuildAssessmentResult(assessments["Systems Final Exam"], student.AccountId, subjectResultsByCode["SJ-SYS"].SubjectResultId, 65m, instructorId, new DateTime(2026, 2, 5)),
            BuildAssessmentResult(assessments["Practical Skills Exam"], student.AccountId, subjectResultsByCode["SJ-PRA"].SubjectResultId, 90m, instructorId, new DateTime(2026, 2, 20)),
            BuildAssessmentResult(assessments["Safety & Human Factors Exam"], student.AccountId, subjectResultsByCode["SJ-SAF"].SubjectResultId, 82m, instructorId, new DateTime(2026, 3, 5)));
        await context.SaveChangesAsync();

        if (!await context.RetakeHistories.AnyAsync())
        {
            // Demonstrates the retake flow: student failed the Systems Final Exam on
            // attempt 1 (65 < 70 passing score) and passed on attempt 2.
            var retakeExam = assessments["Systems Final Exam"];
            var retakeResult = BuildAssessmentResult(retakeExam, student.AccountId, subjectResultsByCode["SJ-SYS"].SubjectResultId, 78m, instructorId, new DateTime(2026, 2, 12));
            retakeResult.AttemptNo = 2;
            context.AssessmentResults.Add(retakeResult);

            context.RetakeHistories.AddRange(
                new RetakeHistory
                {
                    SubjectResultId = subjectResultsByCode["SJ-SYS"].SubjectResultId,
                    RetakeDate = new DateTime(2026, 2, 12),
                    Reason = "Failed first attempt (65 < 70 passing score)",
                    PreviousScore = 65m,
                    NewScore = 78m,
                    AuthorizedByAccountId = instructorId,
                    AttemptNo = 2
                },
                new RetakeHistory
                {
                    SubjectResultId = subjectResultsByCode["SJ-REG"].SubjectResultId,
                    RetakeDate = new DateTime(2026, 1, 16),
                    Reason = "Failed first attempt (60 < 70 passing score)",
                    PreviousScore = 60m,
                    NewScore = 88m,
                    AuthorizedByAccountId = instructorId,
                    AttemptNo = 2
                },
                new RetakeHistory
                {
                    SubjectResultId = subjectResultsByCode["SJ-PRA"].SubjectResultId,
                    RetakeDate = new DateTime(2026, 2, 22),
                    Reason = "Failed practical attempt 1",
                    PreviousScore = 50m,
                    NewScore = 90m,
                    AuthorizedByAccountId = instructorId,
                    AttemptNo = 2
                },
                new RetakeHistory
                {
                    SubjectResultId = subjectResultsByCode["SJ-SAF"].SubjectResultId,
                    RetakeDate = new DateTime(2026, 3, 6),
                    Reason = "Failed attempt 1 (68 < 70)",
                    PreviousScore = 68m,
                    NewScore = 82m,
                    AuthorizedByAccountId = instructorId,
                    AttemptNo = 2
                }
            );
            await context.SaveChangesAsync();
        }
    }

    private static AssessmentResult BuildAssessmentResult(Assessment assessment, int accountId, int subjectResultId, decimal score, int gradedByAccountId, DateTime takenAt)
    {
        return new AssessmentResult
        {
            AssessmentId = assessment.AssessmentId,
            AccountId = accountId,
            SubjectResultId = subjectResultId,
            Score = score,
            ResultStatus = score >= assessment.PassingScore ? "Passed" : "Failed",
            GradedByAccountId = gradedByAccountId,
            RecordedAt = takenAt,
            PublishedAt = takenAt,
            IsPublished = true,
            TakenAt = takenAt,
            AttemptNo = 1
        };
    }

    // ===================== Module: Practical Checklist Results =====================
    // PracticalChecklistResult

    private static async Task SeedPracticalChecklistResultsAsync(AppDbContext context)
    {
        if (await context.PracticalChecklistResults.AnyAsync())
        {
            return;
        }

        var (_, _, _, subjectResultsByCode) = await GetDemoContextAsync(context);
        var qaId = (await context.Accounts.FirstAsync(a => a.Username == QaUsername)).AccountId;
        var checklistItems = await context.PracticalChecklists.OrderBy(pc => pc.DisplayOrder).ToListAsync();
        var practicalSubjectResultId = subjectResultsByCode["SJ-PRA"].SubjectResultId;

        var results = checklistItems.Select(item => new PracticalChecklistResult
        {
            SubjectResultId = practicalSubjectResultId,
            PracticalChecklistId = item.PracticalChecklistId,
            Score = 100m,
            ResultStatus = "Completed",
            VerifiedByAccountId = qaId,
            CompletedAt = new DateTime(2026, 2, 20),
            IsPublished = true,
            PublishedAt = new DateTime(2026, 2, 20)
        }).ToList();

        context.PracticalChecklistResults.AddRange(results);
        await context.SaveChangesAsync();
    }

    // ===================== Module: Signoff =====================
    // SubjectSignoff

    private static async Task SeedSignoffAsync(AppDbContext context)
    {
        if (await context.SubjectSignoffs.AnyAsync())
        {
            return;
        }

        var (_, _, _, subjectResultsByCode) = await GetDemoContextAsync(context);
        var instructorId = (await context.Accounts.FirstAsync(a => a.Username == InstructorUsername)).AccountId;

        var signoffs = subjectResultsByCode.Values.Select(sr => new SubjectSignoff
        {
            SubjectResultId = sr.SubjectResultId,
            SignoffByAccountId = instructorId,
            Role = "Instructor",
            SignoffAt = new DateTime(2026, 4, 25),
            Comment = "All requirements met; subject passed."
        }).ToList();

        context.SubjectSignoffs.AddRange(signoffs);
        await context.SaveChangesAsync();
    }

    // ===================== Module: Evidence =====================
    // EvidenceFile

    private static async Task SeedEvidenceAsync(AppDbContext context)
    {
        if (await context.EvidenceFiles.AnyAsync())
        {
            return;
        }

        var (student, _, _, subjectResultsByCode) = await GetDemoContextAsync(context);
        var instructorId = (await context.Accounts.FirstAsync(a => a.Username == InstructorUsername)).AccountId;
        var qaId = (await context.Accounts.FirstAsync(a => a.Username == QaUsername)).AccountId;
        var evidenceTypeIds = await context.EvidenceTypes.ToDictionaryAsync(et => et.TypeName, et => et.EvidenceTypeId);
        var practicalSubjectResultId = subjectResultsByCode["SJ-PRA"].SubjectResultId;

        context.EvidenceFiles.AddRange(
            new EvidenceFile
            {
                EvidenceTypeId = evidenceTypeIds["Photo Evidence"],
                UploadedByAccountId = instructorId,
                AccountId = student.AccountId,
                SubjectResultId = practicalSubjectResultId,
                FileName = "hydraulic-inspection-photo.jpg",
                FilePath = "/evidence/amt101/hydraulic-inspection-photo.jpg",
                FileExtension = ".jpg",
                MimeType = "image/jpeg",
                FileSize = 245_000,
                VerificationStatus = "Verified",
                VerifiedByAccountId = qaId,
                VerifiedAt = new DateTime(2026, 2, 22),
                UploadedAt = new DateTime(2026, 2, 20)
            },
            new EvidenceFile
            {
                EvidenceTypeId = evidenceTypeIds["Digital Certificate"],
                UploadedByAccountId = instructorId,
                AccountId = student.AccountId,
                SubjectResultId = practicalSubjectResultId,
                FileName = "practical-completion-certificate.pdf",
                FilePath = "/evidence/amt101/practical-completion-certificate.pdf",
                FileExtension = ".pdf",
                MimeType = "application/pdf",
                FileSize = 82_000,
                VerificationStatus = "Verified",
                VerifiedByAccountId = qaId,
                VerifiedAt = new DateTime(2026, 2, 22),
                UploadedAt = new DateTime(2026, 2, 21)
            });
        await context.SaveChangesAsync();
    }

    // ===================== Module: Approval Workflow =====================
    // ApprovalRequest, ApprovalHistory

    private static async Task SeedApprovalWorkflowAsync(AppDbContext context)
    {
        if (await context.ApprovalRequests.AnyAsync())
        {
            return;
        }

        var (_, etr, _, _) = await GetDemoContextAsync(context);
        var instructorId = (await context.Accounts.FirstAsync(a => a.Username == InstructorUsername)).AccountId;
        var managerId = (await context.Accounts.FirstAsync(a => a.Username == ManagerUsername)).AccountId;

        var otherEtrRecords = await context.ETRCourseRecords.Where(e => e.ETRCourseRecordId != etr.ETRCourseRecordId).Take(3).ToListAsync();
        
        var approvalRequest1 = new ApprovalRequest
        {
            ETRCourseRecordId = etr.ETRCourseRecordId,
            CurrentStatus = "Approved",
            SubmittedByAccountId = instructorId,
            SubmittedAt = new DateTime(2026, 5, 2),
            CurrentApproverId = managerId,
            CompletedAt = new DateTime(2026, 5, 10)
        };
        var approvalRequest2 = new ApprovalRequest { ETRCourseRecordId = otherEtrRecords.ElementAtOrDefault(0)?.ETRCourseRecordId ?? 0, CurrentStatus = "Pending", SubmittedByAccountId = instructorId, SubmittedAt = DateTime.UtcNow.AddDays(-1), CurrentApproverId = managerId };
        var approvalRequest3 = new ApprovalRequest { ETRCourseRecordId = otherEtrRecords.ElementAtOrDefault(1)?.ETRCourseRecordId ?? 0, CurrentStatus = "Rejected", SubmittedByAccountId = instructorId, SubmittedAt = DateTime.UtcNow.AddDays(-5), CurrentApproverId = managerId, CompletedAt = DateTime.UtcNow.AddDays(-4) };
        var approvalRequest4 = new ApprovalRequest { ETRCourseRecordId = otherEtrRecords.ElementAtOrDefault(2)?.ETRCourseRecordId ?? 0, CurrentStatus = "UnderReview", SubmittedByAccountId = instructorId, SubmittedAt = DateTime.UtcNow.AddDays(-2), CurrentApproverId = managerId };
        
        // Remove dummy ones where ETRCourseRecordId == 0 just in case
        var requestsToAdd = new List<ApprovalRequest> { approvalRequest1, approvalRequest2, approvalRequest3, approvalRequest4 }
            .Where(r => r.ETRCourseRecordId != 0).ToList();
            
        context.ApprovalRequests.AddRange(requestsToAdd);
        await context.SaveChangesAsync();

        var histories = new List<ApprovalHistory>
        {
            new ApprovalHistory { ApprovalRequestId = approvalRequest1.ApprovalRequestId, ActionByAccountId = instructorId, ActionType = "Submit", NewStatus = "Submitted", ActionAt = new DateTime(2026, 5, 2) },
            new ApprovalHistory { ApprovalRequestId = approvalRequest1.ApprovalRequestId, ActionByAccountId = managerId, ActionType = "Review", PreviousStatus = "Submitted", NewStatus = "UnderReview", ActionAt = new DateTime(2026, 5, 5) },
            new ApprovalHistory { ApprovalRequestId = approvalRequest1.ApprovalRequestId, ActionByAccountId = managerId, ActionType = "Approve", PreviousStatus = "UnderReview", NewStatus = "Approved", ActionAt = new DateTime(2026, 5, 10) }
        };

        if (approvalRequest2.ApprovalRequestId != 0) histories.Add(new ApprovalHistory { ApprovalRequestId = approvalRequest2.ApprovalRequestId, ActionByAccountId = instructorId, ActionType = "Submit", NewStatus = "Submitted", ActionAt = DateTime.UtcNow.AddDays(-1) });
        if (approvalRequest3.ApprovalRequestId != 0)
        {
            histories.Add(new ApprovalHistory { ApprovalRequestId = approvalRequest3.ApprovalRequestId, ActionByAccountId = instructorId, ActionType = "Submit", NewStatus = "Submitted", ActionAt = DateTime.UtcNow.AddDays(-5) });
            histories.Add(new ApprovalHistory { ApprovalRequestId = approvalRequest3.ApprovalRequestId, ActionByAccountId = managerId, ActionType = "Reject", PreviousStatus = "Submitted", NewStatus = "Rejected", ActionAt = DateTime.UtcNow.AddDays(-4) });
        }
        if (approvalRequest4.ApprovalRequestId != 0)
        {
            histories.Add(new ApprovalHistory { ApprovalRequestId = approvalRequest4.ApprovalRequestId, ActionByAccountId = instructorId, ActionType = "Submit", NewStatus = "Submitted", ActionAt = DateTime.UtcNow.AddDays(-2) });
            histories.Add(new ApprovalHistory { ApprovalRequestId = approvalRequest4.ApprovalRequestId, ActionByAccountId = managerId, ActionType = "Review", PreviousStatus = "Submitted", NewStatus = "UnderReview", ActionAt = DateTime.UtcNow.AddDays(-1) });
        }

        context.ApprovalHistories.AddRange(histories);
        await context.SaveChangesAsync();

        // Manager approval completes and locks the ETR record (domain.md step 6).
        etr.Status = "Completed";
        etr.SubmittedAt = new DateTime(2026, 5, 2);
        etr.VerifiedAt = new DateTime(2026, 5, 5);
        etr.CompletedAt = new DateTime(2026, 5, 10);
        etr.IsLocked = true;
        await context.SaveChangesAsync();
    }

    // ===================== Module: Miscellaneous =====================
    // AmendmentRequest, AuditLog, ExportJob

    private static async Task SeedMiscellaneousAsync(AppDbContext context)
    {
        var (student, etr, _, subjectResultsByCode) = await GetDemoContextAsync(context);
        var instructorId = (await context.Accounts.FirstAsync(a => a.Username == InstructorUsername)).AccountId;
        var managerId = (await context.Accounts.FirstAsync(a => a.Username == ManagerUsername)).AccountId;
        var qaId = (await context.Accounts.FirstAsync(a => a.Username == QaUsername)).AccountId;

        // AmendmentRequests
        if (await context.AmendmentRequests.CountAsync() < 4)
        {
            context.AmendmentRequests.RemoveRange(context.AmendmentRequests);
            await context.SaveChangesAsync();
            var srId = subjectResultsByCode["SJ-REG"].SubjectResultId;
            context.AmendmentRequests.AddRange(
                new AmendmentRequest
                {
                    SubjectResultId = srId,
                    RequestedByAccountId = instructorId,
                    Reason = "Typo in initial score entry",
                    OldValue = "Passed",
                    Status = "Pending"
                },
                new AmendmentRequest
                {
                    SubjectResultId = subjectResultsByCode["SJ-SYS"].SubjectResultId,
                    RequestedByAccountId = instructorId,
                    Reason = "Wrong checklist submitted",
                    OldValue = "Passed",
                    Status = "Rejected",
                    ApprovedByAccountId = managerId,
                    ApprovedAt = DateTime.UtcNow.AddDays(-1),
                    DecisionComment = "Provide more proof before reopening."
                },
                new AmendmentRequest
                {
                    SubjectResultId = subjectResultsByCode["SJ-PRA"].SubjectResultId,
                    RequestedByAccountId = instructorId,
                    Reason = "Re-evaluation requested by student",
                    OldValue = "Failed",
                    Status = "Approved",
                    ApprovedByAccountId = managerId,
                    ApprovedAt = DateTime.UtcNow.AddDays(-2),
                    DecisionComment = "Approved for re-evaluation."
                },
                new AmendmentRequest
                {
                    SubjectResultId = subjectResultsByCode["SJ-SAF"].SubjectResultId,
                    RequestedByAccountId = instructorId,
                    Reason = "System error during sync",
                    OldValue = "Failed",
                    Status = "Pending"
                }
            );
            await context.SaveChangesAsync();
        }

        // AuditLogs
        if (await context.AuditLogs.CountAsync() < 4)
        {
            context.AuditLogs.RemoveRange(context.AuditLogs);
            await context.SaveChangesAsync();
            context.AuditLogs.AddRange(
                new AuditLog
                {
                    AccountId = instructorId,
                    ActionType = "Login",
                    EntityName = "Account",
                    RecordId = instructorId,
                    Description = "Instructor logged in",
                    IPAddress = "192.168.1.10",
                    UserAgent = "Mozilla/5.0",
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new AuditLog
                {
                    AccountId = managerId,
                    ActionType = "Approve",
                    EntityName = "ETRCourseRecord",
                    RecordId = etr.ETRCourseRecordId,
                    Description = "Manager approved ETR",
                    IPAddress = "192.168.1.12",
                    UserAgent = "Mozilla/5.0",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new AuditLog
                {
                    AccountId = qaId,
                    ActionType = "Verify",
                    EntityName = "EvidenceFile",
                    RecordId = 1,
                    Description = "QA verified evidence",
                    IPAddress = "192.168.1.15",
                    UserAgent = "Chrome/114",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new AuditLog
                {
                    AccountId = instructorId,
                    ActionType = "Update",
                    EntityName = "SubjectResult",
                    RecordId = subjectResultsByCode["SJ-REG"].SubjectResultId,
                    Description = "Updated score after amendment",
                    IPAddress = "192.168.1.10",
                    UserAgent = "Mozilla/5.0",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            );
            await context.SaveChangesAsync();
        }

        // ExportJobs
        if (await context.ExportJobs.CountAsync() < 4)
        {
            context.ExportJobs.RemoveRange(context.ExportJobs);
            await context.SaveChangesAsync();
            context.ExportJobs.AddRange(
                new ExportJob
                {
                    RequestedByAccountId = managerId,
                    ExportType = "ETRReport",
                    FileName = "ETR_Report_AMT101.pdf",
                    FilePath = "/exports/ETR_Report_AMT101.pdf",
                    Status = "Completed",
                    RequestedAt = DateTime.UtcNow.AddDays(-1),
                    CompletedAt = DateTime.UtcNow.AddDays(-1),
                    ETRCourseRecordId = etr.ETRCourseRecordId
                },
                new ExportJob
                {
                    RequestedByAccountId = qaId,
                    ExportType = "AuditReport",
                    FileName = null,
                    FilePath = null,
                    Status = "Processing",
                    RequestedAt = DateTime.UtcNow.AddMinutes(-30),
                    CompletedAt = null,
                    ETRCourseRecordId = null
                },
                new ExportJob
                {
                    RequestedByAccountId = managerId,
                    ExportType = "TraineeProgress",
                    FileName = "Progress_AMT101.xlsx",
                    FilePath = null,
                    Status = "Failed",
                    RequestedAt = DateTime.UtcNow.AddDays(-2),
                    CompletedAt = DateTime.UtcNow.AddDays(-2),
                    ETRCourseRecordId = null
                },
                new ExportJob
                {
                    RequestedByAccountId = instructorId,
                    ExportType = "ClassRoster",
                    FileName = "Roster_AMT101.csv",
                    FilePath = "/exports/Roster_AMT101.csv",
                    Status = "Completed",
                    RequestedAt = DateTime.UtcNow.AddHours(-5),
                    CompletedAt = DateTime.UtcNow.AddHours(-4),
                    ETRCourseRecordId = null
                }
            );
            await context.SaveChangesAsync();
        }
    }
}
