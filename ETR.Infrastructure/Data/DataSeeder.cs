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
                new Role { RoleName = "Audit", Description = "Auditor" });
            await context.SaveChangesAsync();
        }

        if (!await context.Departments.AnyAsync())
        {
            context.Departments.AddRange(
                new Department { DepartmentName = "Administration", Description = "General Admin" },
                new Department { DepartmentName = "Training", Description = "Training Dept" });
            await context.SaveChangesAsync();
        }

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
                CreateAccount("audit@etr.com", roleIds["Audit"], deptIds["Administration"], "AUD-01", "Audit Staff", new DateTime(1985, 1, 1), "Other"));
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
            context.Courses.Add(new Course
            {
                CourseCode = CourseCode,
                CourseName = "Aircraft Maintenance Technician - Basic",
                Description = "Foundational course covering regulations, aircraft systems, practical maintenance skills, and safety for entry-level maintenance technicians.",
                DurationHours = 120,
                Status = "Active"
            });
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
                new CompletionRequirement { CourseId = course.CourseId, RequirementName = "All Practical Checklists Signed Off", IsMandatory = true, DisplayOrder = 3, RequirementType = "AllChecklistsSignedOff" });
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
            context.Classes.Add(new Class
            {
                ClassCode = ClassCode,
                ClassName = "AMT-101 Batch 1",
                CourseId = course.CourseId,
                StartDate = new DateTime(2026, 1, 5),
                EndDate = new DateTime(2026, 4, 30),
                Location = "Hangar 3 Training Center",
                Capacity = 20,
                Status = "Completed"
            });
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
    // CourseEnrollment, ClassStudent

    private static async Task SeedEnrollmentAsync(AppDbContext context)
    {
        var cls = await context.Classes.FirstAsync(c => c.ClassCode == ClassCode);
        var student = await context.Accounts.FirstAsync(a => a.Username == StudentUsername);

        if (!await context.CourseEnrollments.AnyAsync())
        {
            context.CourseEnrollments.Add(new CourseEnrollment
            {
                AccountId = student.AccountId,
                ClassId = cls.ClassId,
                Status = "Completed",
                EnrolledAt = new DateTime(2026, 1, 5),
                StartDate = new DateTime(2026, 1, 5),
                ExpectedCompletionDate = new DateTime(2026, 4, 30),
                ActualCompletionDate = new DateTime(2026, 4, 30)
            });
            await context.SaveChangesAsync();
        }

        if (!await context.ClassStudents.AnyAsync())
        {
            var enrollment = await context.CourseEnrollments.FirstAsync(e => e.AccountId == student.AccountId && e.ClassId == cls.ClassId);
            context.ClassStudents.Add(new ClassStudent
            {
                CourseEnrollmentId = enrollment.EnrollmentId,
                ClassId = cls.ClassId,
                AccountId = student.AccountId,
                Status = "Completed"
            });
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
            // Created unlocked/in-progress here; the Approval module below moves it to
            // Completed + locked once the (seeded) manager approval trail exists —
            // mirroring the real submit -> approve -> lock workflow (see domain.md).
            context.ETRCourseRecords.Add(new ETRCourseRecord
            {
                EnrollmentId = enrollment.EnrollmentId,
                Status = "InProgress",
                IsLocked = false,
                CreatedBySystem = true
            });
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
    private static async Task<(Account Student, ETRCourseRecord Etr, ClassStudent ClassStudent, Dictionary<string, SubjectResult> SubjectResultsByCode)> GetDemoContextAsync(AppDbContext context)
    {
        var student = await context.Accounts.FirstAsync(a => a.Username == StudentUsername);
        var cls = await context.Classes.FirstAsync(c => c.ClassCode == ClassCode);
        var enrollment = await context.CourseEnrollments.FirstAsync(e => e.AccountId == student.AccountId && e.ClassId == cls.ClassId);
        var etr = await context.ETRCourseRecords.FirstAsync(e => e.EnrollmentId == enrollment.EnrollmentId);
        var classStudent = await context.ClassStudents.FirstAsync(cs => cs.AccountId == student.AccountId && cs.ClassId == cls.ClassId);

        var subjectCodesById = await context.Subjects.ToDictionaryAsync(s => s.SubjectId, s => s.SubjectCode);
        var subjectResultsByCode = await context.SubjectResults
            .Where(sr => sr.EtrId == etr.ETRCourseRecordId)
            .ToDictionaryAsync(sr => subjectCodesById[sr.SubjectId], sr => sr);

        return (student, etr, classStudent, subjectResultsByCode);
    }

    // ===================== Module: Attendance =====================
    // AttendanceRecord

    private static async Task SeedAttendanceAsync(AppDbContext context)
    {
        if (await context.AttendanceRecords.AnyAsync())
        {
            return;
        }

        var (_, _, classStudent, _) = await GetDemoContextAsync(context);
        var instructorId = (await context.Accounts.FirstAsync(a => a.Username == InstructorUsername)).AccountId;
        var sessions = await context.Sessions.Where(s => s.IsConfirmed).ToListAsync();

        var records = sessions.Select(s => new AttendanceRecord
        {
            SessionId = s.SessionId,
            ClassStudentId = classStudent.ClassStudentId,
            Status = "Present",
            RecordedByAccountId = instructorId,
            RecordedAt = s.SessionDate
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

            context.RetakeHistories.Add(new RetakeHistory
            {
                SubjectResultId = subjectResultsByCode["SJ-SYS"].SubjectResultId,
                RetakeDate = new DateTime(2026, 2, 12),
                Reason = "Failed first attempt (65 < 70 passing score)",
                PreviousScore = 65m,
                NewScore = 78m,
                AuthorizedByAccountId = instructorId,
                AttemptNo = 2
            });
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

        var approvalRequest = new ApprovalRequest
        {
            ETRCourseRecordId = etr.ETRCourseRecordId,
            CurrentStatus = "Approved",
            SubmittedByAccountId = instructorId,
            SubmittedAt = new DateTime(2026, 5, 2),
            CurrentApproverId = managerId,
            CompletedAt = new DateTime(2026, 5, 10)
        };
        context.ApprovalRequests.Add(approvalRequest);
        await context.SaveChangesAsync();

        context.ApprovalHistories.AddRange(
            new ApprovalHistory { ApprovalRequestId = approvalRequest.ApprovalRequestId, ActionByAccountId = instructorId, ActionType = "Submit", NewStatus = "Submitted", ActionAt = new DateTime(2026, 5, 2) },
            new ApprovalHistory { ApprovalRequestId = approvalRequest.ApprovalRequestId, ActionByAccountId = managerId, ActionType = "Review", PreviousStatus = "Submitted", NewStatus = "UnderReview", ActionAt = new DateTime(2026, 5, 5) },
            new ApprovalHistory { ApprovalRequestId = approvalRequest.ApprovalRequestId, ActionByAccountId = managerId, ActionType = "Approve", PreviousStatus = "UnderReview", NewStatus = "Approved", ActionAt = new DateTime(2026, 5, 10) });
        await context.SaveChangesAsync();

        // Manager approval completes and locks the ETR record (domain.md step 6).
        etr.Status = "Completed";
        etr.SubmittedAt = new DateTime(2026, 5, 2);
        etr.VerifiedAt = new DateTime(2026, 5, 5);
        etr.CompletedAt = new DateTime(2026, 5, 10);
        etr.IsLocked = true;
        await context.SaveChangesAsync();
    }
}
