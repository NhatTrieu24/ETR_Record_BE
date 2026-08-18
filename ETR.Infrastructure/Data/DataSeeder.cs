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
            new Department { DepartmentName = "Administration" },
            new Department { DepartmentName = "Training" },
            new Department { DepartmentName = "Flight Crew" },
            new Department { DepartmentName = "Cabin Crew" },
            new Department { DepartmentName = "Engineering & Maintenance" },
            new Department { DepartmentName = "Ground Operations" }
        };
        foreach (var dept in defaultDepartments)
        {
            if (!await context.Departments.AnyAsync(d => d.DepartmentName == dept.DepartmentName))
                context.Departments.Add(dept);
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
            context.Courses.Add(new Course { CourseCode = "AMT-101", CourseName = "Aircraft Maintenance Technician", DurationHours = 120, Status = CourseStatus.Active });
            context.Courses.Add(new Course { CourseCode = "B737-TR", CourseName = "B737 Type Rating", DurationHours = 160, Status = CourseStatus.Active });
            context.Courses.Add(new Course { CourseCode = "A320-FAM", CourseName = "A320 Familiarization", DurationHours = 40, Status = CourseStatus.Active });
            context.Courses.Add(new Course { CourseCode = "ENG-101", CourseName = "Aviation English", DurationHours = 60, Status = CourseStatus.Active });
            context.Courses.Add(new Course { CourseCode = "SMS-101", CourseName = "Safety Management Systems", DurationHours = 20, Status = CourseStatus.Active });
            context.Courses.Add(new Course { CourseCode = "HF-101", CourseName = "Human Factors", DurationHours = 15, Status = CourseStatus.Active });
            context.Courses.Add(new Course { CourseCode = "A350-TR", CourseName = "A350 Type Rating", DurationHours = 160, Status = CourseStatus.Active });
            context.Courses.Add(new Course { CourseCode = "B787-TR", CourseName = "B787 Type Rating", DurationHours = 160, Status = CourseStatus.Active });
            context.Courses.Add(new Course { CourseCode = "DGR-101", CourseName = "Dangerous Goods Regulations", DurationHours = 10, Status = CourseStatus.Active });
            context.Courses.Add(new Course { CourseCode = "SEC-101", CourseName = "Aviation Security", DurationHours = 10, Status = CourseStatus.Active });
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
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["AMT-101"], SubjectId = subjects["SJ-REG"], SequenceNo = 1, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["AMT-101"], SubjectId = subjects["SJ-SYS"], SequenceNo = 2, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["AMT-101"], SubjectId = subjects["SJ-PRA"], SequenceNo = 3, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["AMT-101"], SubjectId = subjects["SJ-SAF"], SequenceNo = 4, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["AMT-101"], SubjectId = subjects["SJ-ENG"], SequenceNo = 5, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["B737-TR"], SubjectId = subjects["SJ-REG"], SequenceNo = 1, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["B737-TR"], SubjectId = subjects["SJ-SYS"], SequenceNo = 2, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["B737-TR"], SubjectId = subjects["SJ-PRA"], SequenceNo = 3, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["B737-TR"], SubjectId = subjects["SJ-SAF"], SequenceNo = 4, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["B737-TR"], SubjectId = subjects["SJ-ENG"], SequenceNo = 5, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["A320-FAM"], SubjectId = subjects["SJ-REG"], SequenceNo = 1, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["A320-FAM"], SubjectId = subjects["SJ-SYS"], SequenceNo = 2, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["A320-FAM"], SubjectId = subjects["SJ-PRA"], SequenceNo = 3, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
            context.CourseSubjects.Add(new CourseSubject { CourseId = courses["A320-FAM"], SubjectId = subjects["SJ-SAF"], SequenceNo = 4, RequiredHours = 10, PassingScore = 70, IsMandatory = true, SubjectVersion = "1.0" });
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
                new EvidenceType { TypeName = "Photo Evidence" },
                new EvidenceType { TypeName = "Signed Paper Form" },
                new EvidenceType { TypeName = "Digital Certificate" });
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
            foreach(var cls in classEntities)
            {
                var subjectsForCourse = courseSubjects.Where(cs => cs.CourseId == cls.CourseId).ToList();
                foreach(var sub in subjectsForCourse)
                {
                    var instructorId = instructorIds[rand.Next(instructorIds.Count)];
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

            foreach(var cs in classSubjects)
            {
                var assessment = assessments.FirstOrDefault(a => a.SubjectId == cs.SubjectId);
                var checklist = checklists.FirstOrDefault(c => c.SubjectId == cs.SubjectId);

                // Session 1 is confirmed, Session 2 is unconfirmed so instructor can test attendance
                context.Sessions.Add(new Session { ClassId = cs.ClassId, SubjectId = cs.SubjectId, SessionTitle = "Session 1", SessionDate = DateTime.UtcNow.AddDays(-10), IsConfirmed = true, ConfirmedByAccountId = cs.InstructorAccountId });
                context.Sessions.Add(new Session { ClassId = cs.ClassId, SubjectId = cs.SubjectId, SessionTitle = "Session 2", SessionDate = DateTime.UtcNow.AddDays(2), IsConfirmed = false, ConfirmedByAccountId = null });
                
                // Add exam sessions
                if (assessment != null)
                {
                    context.Sessions.Add(new Session { ClassId = cs.ClassId, SubjectId = cs.SubjectId, SessionTitle = "Theory Exam", SessionDate = DateTime.UtcNow.AddDays(4), IsConfirmed = false, ConfirmedByAccountId = null, IsAssessmentRequired = true, AssessmentId = assessment.AssessmentId });
                }
                
                if (checklist != null)
                {
                    context.Sessions.Add(new Session { ClassId = cs.ClassId, SubjectId = cs.SubjectId, SessionTitle = "Practical Exam", SessionDate = DateTime.UtcNow.AddDays(5), IsConfirmed = false, ConfirmedByAccountId = null, IsChecklistRequired = true, PracticalChecklistId = checklist.PracticalChecklistId });
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
            var rand = new Random(42);
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

            // Create physical directory
            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "evidences");
            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            // Generate two dummy files
            var dummyPdfPath = Path.Combine(uploadDir, "dummy_evidence.pdf");
            if (!File.Exists(dummyPdfPath)) {
                // A very basic valid PDF structure
                string pdfContent = "%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n3 0 obj\n<< /Type /Page /Parent 2 0 R /Resources << /Font << /F1 4 0 R >> >> /MediaBox [0 0 612 792] /Contents 5 0 R >>\nendobj\n4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n5 0 obj\n<< /Length 44 >>\nstream\nBT /F1 24 Tf 100 700 Td (Dummy Evidence File) Tj ET\nendstream\nendobj\nxref\n0 6\n0000000000 65535 f \n0000000009 00000 n \n0000000058 00000 n \n0000000115 00000 n \n0000000219 00000 n \n0000000307 00000 n \ntrailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n402\n%%EOF";
                File.WriteAllText(dummyPdfPath, pdfContent);
            }
            
            var dummyImgPath = Path.Combine(uploadDir, "dummy_evidence.jpg");
            if (!File.Exists(dummyImgPath)) {
                // Minimal 1x1 valid JPEG
                byte[] jpegContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x01, 0x00, 0x48, 0x00, 0x48, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xC0, 0x00, 0x0B, 0x08, 0x00, 0x01, 0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0xFF, 0xC4, 0x00, 0x14, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xC4, 0x00, 0x14, 0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x3F, 0x00, 0x37, 0xFF, 0xD9 };
                File.WriteAllBytes(dummyImgPath, jpegContent);
            }

            // Take just a sample of SubjectResults to avoid inserting thousands of evidences which slows down EF
            var sampleResults = subjectResults.OrderBy(x => rand.Next()).Take(50).ToList();

            foreach(var sr in sampleResults)
            {
                var etr = await context.ETRCourseRecords.FirstAsync(e => e.ETRCourseRecordId == sr.EtrId);
                var enrollment = await context.CourseEnrollments.FirstAsync(e => e.EnrollmentId == etr.EnrollmentId);
                
                context.EvidenceFiles.Add(new EvidenceFile
                {
                    EvidenceTypeId = evidenceTypes[0].EvidenceTypeId, // Photo
                    UploadedByAccountId = sr.EvaluatedByAccountId ?? 1,
                    AccountId = enrollment.AccountId,
                    SubjectResultId = sr.SubjectResultId,
                    FileName = "dummy_evidence.jpg",
                    FilePath = "uploads/evidences/dummy_evidence.jpg",
                    FileExtension = ".jpg",
                    MimeType = "image/jpeg",
                    FileSize = 135,
                    VerificationStatus = "Verified",
                    VerifiedByAccountId = qaId,
                    VerifiedAt = DateTime.UtcNow,
                    UploadedAt = DateTime.UtcNow
                });
                
                context.EvidenceFiles.Add(new EvidenceFile
                {
                    EvidenceTypeId = evidenceTypes[2].EvidenceTypeId, // PDF
                    UploadedByAccountId = sr.EvaluatedByAccountId ?? 1,
                    AccountId = enrollment.AccountId,
                    SubjectResultId = sr.SubjectResultId,
                    FileName = "dummy_evidence.pdf",
                    FilePath = "uploads/evidences/dummy_evidence.pdf",
                    FileExtension = ".pdf",
                    MimeType = "application/pdf",
                    FileSize = 402,
                    VerificationStatus = "Verified",
                    VerifiedByAccountId = qaId,
                    VerifiedAt = DateTime.UtcNow,
                    UploadedAt = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedApprovalWorkflowAsync(AppDbContext context)
    {
        if (!await context.ApprovalRequests.AnyAsync())
        {
            var etrs = await context.ETRCourseRecords.ToListAsync();
            var managerId = (await context.Accounts.FirstAsync(a => a.Username == ManagerUsername)).AccountId;
            var instructorId = (await context.Accounts.FirstAsync(a => a.Username == InstructorUsername)).AccountId;
            var rand = new Random(42);

            var sampleEtrs = etrs.OrderBy(x => rand.Next()).Take(30).ToList();
            
            foreach(var etr in sampleEtrs)
            {
                var statuses = new[] { "Pending", "UnderReview", "Approved", "Rejected" };
                var status = statuses[rand.Next(statuses.Length)];
                
                var request = new ApprovalRequest
                {
                    ETRCourseRecordId = etr.ETRCourseRecordId,
                    CurrentStatus = status,
                    SubmittedByAccountId = instructorId,
                    SubmittedAt = DateTime.UtcNow.AddDays(-5),
                    CurrentApproverId = managerId,
                    CompletedAt = (status == "Approved" || status == "Rejected") ? DateTime.UtcNow.AddDays(-1) : null
                };
                context.ApprovalRequests.Add(request);
                await context.SaveChangesAsync(); // Save to get ID
                
                context.ApprovalHistories.Add(new ApprovalHistory
                {
                    ApprovalRequestId = request.ApprovalRequestId,
                    ActionByAccountId = instructorId,
                    ActionType = ApprovalHistoryActionType.Submit.ToString(),
                    NewStatus = "Pending",
                    ActionAt = DateTime.UtcNow.AddDays(-5)
                });
                
                if (status == "UnderReview" || status == "Approved" || status == "Rejected")
                {
                    context.ApprovalHistories.Add(new ApprovalHistory
                    {
                        ApprovalRequestId = request.ApprovalRequestId,
                        ActionByAccountId = managerId,
                        ActionType = ApprovalHistoryActionType.Review.ToString(),
                        PreviousStatus = "Pending",
                        NewStatus = "UnderReview",
                        ActionAt = DateTime.UtcNow.AddDays(-3)
                    });
                }
                
                if (status == "Approved")
                {
                    context.ApprovalHistories.Add(new ApprovalHistory
                    {
                        ApprovalRequestId = request.ApprovalRequestId,
                        ActionByAccountId = managerId,
                        ActionType = ApprovalHistoryActionType.Approve.ToString(),
                        PreviousStatus = "UnderReview",
                        NewStatus = "Approved",
                        ActionAt = DateTime.UtcNow.AddDays(-1)
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
                        ActionAt = DateTime.UtcNow.AddDays(-1)
                    });
                }
            }
            await context.SaveChangesAsync();
        }
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
}
