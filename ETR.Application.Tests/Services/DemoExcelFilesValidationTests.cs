using ClosedXML.Excel;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace ETR.Application.Tests.Services;

public class DemoExcelFilesValidationTests
{
    private readonly ITestOutputHelper _output;

    public DemoExcelFilesValidationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static (Mock<IUnitOfWork> uow, Mock<IClassService> clsSvc, Mock<IEnrollmentService> enrSvc) BuildSeededMocks()
    {
        var uow = new Mock<IUnitOfWork>();
        var clsSvc = new Mock<IClassService>();
        var enrSvc = new Mock<IEnrollmentService>();

        // 1. Roles
        var roles = new List<Role>
        {
            new() { RoleId = 1, RoleName = "Admin" },
            new() { RoleId = 2, RoleName = "Instructor" },
            new() { RoleId = 3, RoleName = "QA" },
            new() { RoleId = 4, RoleName = "Academic" },
            new() { RoleId = 5, RoleName = "TrainingManager" },
            new() { RoleId = 6, RoleName = "Student" },
            new() { RoleId = 7, RoleName = "Audit" },
            new() { RoleId = 8, RoleName = "ManagementViewer" }
        };
        var roleRepo = new Mock<IGenericRepository<Role>>();
        roleRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(roles);
        uow.Setup(u => u.RoleRepository).Returns(roleRepo.Object);

        // 2. Departments
        var departments = new List<Department>
        {
            new() { DepartmentId = 1, DepartmentName = "Administration", IsDeleted = false },
            new() { DepartmentId = 2, DepartmentName = "Training", IsDeleted = false },
            new() { DepartmentId = 3, DepartmentName = "Flight Crew", IsDeleted = false },
            new() { DepartmentId = 4, DepartmentName = "Cabin Crew", IsDeleted = false },
            new() { DepartmentId = 5, DepartmentName = "Engineering & Maintenance", IsDeleted = false },
            new() { DepartmentId = 6, DepartmentName = "Ground Operations", IsDeleted = false }
        };
        var deptRepo = new Mock<IGenericRepository<Department>>();
        deptRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(departments);
        uow.Setup(u => u.DepartmentRepository).Returns(deptRepo.Object);

        // 3. Accounts
        var accounts = new List<Account>
        {
            new() { AccountId = 1, Username = "admin@etr.com", RoleId = 1, DepartmentId = 1, Status = AccountStatus.Active, IsActive = true },
            new() { AccountId = 2, Username = "instructor@etr.com", RoleId = 2, DepartmentId = 2, Status = AccountStatus.Active, IsActive = true },
            new() { AccountId = 3, Username = "qa@etr.com", RoleId = 3, DepartmentId = 1, Status = AccountStatus.Active, IsActive = true },
            new() { AccountId = 4, Username = "academic@etr.com", RoleId = 4, DepartmentId = 1, Status = AccountStatus.Active, IsActive = true },
            new() { AccountId = 5, Username = "student@etr.com", RoleId = 6, DepartmentId = 2, Status = AccountStatus.Active, IsActive = true },
            new() { AccountId = 6, Username = "manager@etr.com", RoleId = 5, DepartmentId = 2, Status = AccountStatus.Active, IsActive = true },
            new() { AccountId = 7, Username = "auditor@etr.com", RoleId = 7, DepartmentId = 1, Status = AccountStatus.Active, IsActive = true },
            new() { AccountId = 8, Username = "viewer@etr.com", RoleId = 8, DepartmentId = 1, Status = AccountStatus.Active, IsActive = true },
            new() { AccountId = 9, Username = "student2@etr.com", RoleId = 6, DepartmentId = 2, Status = AccountStatus.Active, IsActive = true },
            new() { AccountId = 10, Username = "student3@etr.com", RoleId = 6, DepartmentId = 2, Status = AccountStatus.Active, IsActive = true },
            new() { AccountId = 11, Username = "student4@etr.com", RoleId = 6, DepartmentId = 2, Status = AccountStatus.Active, IsActive = true },
            new() { AccountId = 12, Username = "instructor2@etr.com", RoleId = 2, DepartmentId = 2, Status = AccountStatus.Active, IsActive = true }
        };
        var accRepo = new Mock<IGenericRepository<Account>>();
        accRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(accounts);
        uow.Setup(u => u.AccountRepository).Returns(accRepo.Object);

        // 4. UserProfiles
        var profiles = new List<UserProfile>
        {
            new() { AccountId = 1, UserCode = "ADM-001", FullName = "System Administrator", Email = "admin@etr.com", Status = LearnerStatus.Active },
            new() { AccountId = 2, UserCode = "INS-001", FullName = "Capt. Tran Van Thanh", Email = "instructor@etr.com", Status = LearnerStatus.Active },
            new() { AccountId = 3, UserCode = "QA-001", FullName = "Le Thi Mai", Email = "qa@etr.com", Status = LearnerStatus.Active },
            new() { AccountId = 4, UserCode = "ACA-001", FullName = "Nguyen Hoang Long", Email = "academic@etr.com", Status = LearnerStatus.Active },
            new() { AccountId = 5, UserCode = "STU-01", FullName = "Jane Student", Email = "student@etr.com", Status = LearnerStatus.Active },
            new() { AccountId = 6, UserCode = "MGR-001", FullName = "Doan Minh Tri", Email = "manager@etr.com", Status = LearnerStatus.Active },
            new() { AccountId = 7, UserCode = "AUD-001", FullName = "Pham Quang Huy", Email = "auditor@etr.com", Status = LearnerStatus.Active },
            new() { AccountId = 8, UserCode = "USR-001", FullName = "Vuong Dinh Khoi", Email = "viewer@etr.com", Status = LearnerStatus.Active },
            new() { AccountId = 9, UserCode = "STU-02", FullName = "Student 2", Email = "student2@etr.com", Status = LearnerStatus.Active },
            new() { AccountId = 10, UserCode = "STU-03", FullName = "Student 3", Email = "student3@etr.com", Status = LearnerStatus.Active },
            new() { AccountId = 11, UserCode = "STU-04", FullName = "Student 4", Email = "student4@etr.com", Status = LearnerStatus.Active },
            new() { AccountId = 12, UserCode = "INS-002", FullName = "Capt. Nguyen Quoc Bao", Email = "instructor2@etr.com", Status = LearnerStatus.Active }
        };
        var profRepo = new Mock<IGenericRepository<UserProfile>>();
        profRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
        uow.Setup(u => u.UserProfileRepository).Returns(profRepo.Object);

        // 5. Courses
        var courses = new List<Course>
        {
            new() { CourseId = 1, CourseCode = "AMT-BASIC-2026", CourseName = "Aircraft Maintenance Technician Initial Qualification", Status = CourseStatus.Active, IsDeleted = false },
            new() { CourseId = 2, CourseCode = "A320-FAM", CourseName = "Airbus A320 Familiarization and Type Rating", Status = CourseStatus.Active, IsDeleted = false },
            new() { CourseId = 3, CourseCode = "B737-RATING", CourseName = "Boeing 737 Next Generation Type Rating", Status = CourseStatus.Active, IsDeleted = false }
        };
        var courseRepo = new Mock<IGenericRepository<Course>>();
        courseRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(courses);
        uow.Setup(u => u.CourseRepository).Returns(courseRepo.Object);

        // 6. Subjects
        var subjects = new List<Subject>
        {
            new() { SubjectId = 1, SubjectCode = "SJ-REG", SubjectName = "Aviation Regulations", SubjectType = "Theory", IsDeleted = false },
            new() { SubjectId = 2, SubjectCode = "SJ-SYS", SubjectName = "Aircraft Systems & Avionics", SubjectType = "Theory", IsDeleted = false },
            new() { SubjectId = 3, SubjectCode = "SJ-PRA", SubjectName = "Practical Line Maintenance & Troubleshooting", SubjectType = "Practical", IsDeleted = false },
            new() { SubjectId = 4, SubjectCode = "SJ-HF", SubjectName = "Human Factors & Safety Management", SubjectType = "Theory", IsDeleted = false }
        };
        var subjRepo = new Mock<IGenericRepository<Subject>>();
        subjRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(subjects);
        subjRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(subjects[0]);
        uow.Setup(u => u.SubjectRepository).Returns(subjRepo.Object);

        // 7. CourseSubjects
        var courseSubjects = new List<CourseSubject>
        {
            new() { CourseId = 1, SubjectId = 1, SequenceNo = 1, RequiredHours = 30, RequiredSessions = 6, PassingScore = 70.00m, IsMandatory = true, IsDeleted = false },
            new() { CourseId = 1, SubjectId = 2, SequenceNo = 2, RequiredHours = 40, RequiredSessions = 8, PassingScore = 75.00m, IsMandatory = true, IsDeleted = false },
            new() { CourseId = 1, SubjectId = 3, SequenceNo = 3, RequiredHours = 30, RequiredSessions = 6, PassingScore = 80.00m, IsMandatory = true, IsDeleted = false },
            new() { CourseId = 1, SubjectId = 4, SequenceNo = 4, RequiredHours = 20, RequiredSessions = 4, PassingScore = 70.00m, IsMandatory = true, IsDeleted = false },
            new() { CourseId = 2, SubjectId = 2, SequenceNo = 1, RequiredHours = 40, RequiredSessions = 8, PassingScore = 75.00m, IsMandatory = true, IsDeleted = false },
            new() { CourseId = 2, SubjectId = 3, SequenceNo = 2, RequiredHours = 40, RequiredSessions = 8, PassingScore = 80.00m, IsMandatory = true, IsDeleted = false },
            new() { CourseId = 3, SubjectId = 2, SequenceNo = 1, RequiredHours = 50, RequiredSessions = 10, PassingScore = 75.00m, IsMandatory = true, IsDeleted = false },
            new() { CourseId = 3, SubjectId = 3, SequenceNo = 2, RequiredHours = 40, RequiredSessions = 8, PassingScore = 80.00m, IsMandatory = true, IsDeleted = false }
        };
        var csRepo = new Mock<IGenericRepository<CourseSubject>>();
        csRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(courseSubjects);
        uow.Setup(u => u.CourseSubjectRepository).Returns(csRepo.Object);

        // 8. Classes
        var classes = new List<Class>
        {
            new() { ClassId = 1, ClassCode = "AMT-101-C1", ClassName = "AMT Initial Qualification Batch 2026-A", CourseId = 1, StartDate = new DateTime(2026, 8, 1), EndDate = new DateTime(2026, 11, 30), Status = ClassStatus.InProgress, Capacity = 25, IsDeleted = false },
            new() { ClassId = 2, ClassCode = "AMT-101-C2", ClassName = "AMT Initial Qualification Batch 2026-B", CourseId = 1, StartDate = new DateTime(2026, 3, 1), EndDate = new DateTime(2026, 6, 30), Status = ClassStatus.Completed, Capacity = 20, IsDeleted = false },
            new() { ClassId = 3, ClassCode = "B737-2026A", ClassName = "B737 NG Rating Batch 01", CourseId = 3, StartDate = new DateTime(2026, 11, 1), EndDate = new DateTime(2027, 2, 28), Status = ClassStatus.Scheduled, Capacity = 16, IsDeleted = false }
        };
        var clsRepo = new Mock<IGenericRepository<Class>>();
        clsRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(classes);
        clsRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(classes[0]);
        uow.Setup(u => u.ClassRepository).Returns(clsRepo.Object);

        // 9. CourseEnrollments (Class 1)
        var enrollments = new List<CourseEnrollment>
        {
            new() { EnrollmentId = 1, AccountId = 5, ClassId = 1, Status = EnrollmentStatus.Active, IsDeleted = false },
            new() { EnrollmentId = 2, AccountId = 9, ClassId = 1, Status = EnrollmentStatus.Active, IsDeleted = false },
            new() { EnrollmentId = 3, AccountId = 10, ClassId = 1, Status = EnrollmentStatus.Active, IsDeleted = false },
            new() { EnrollmentId = 4, AccountId = 11, ClassId = 1, Status = EnrollmentStatus.Active, IsDeleted = false }
        };
        var enrRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        enrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(enrollments);
        uow.Setup(u => u.CourseEnrollmentRepository).Returns(enrRepo.Object);

        // 10. ETRCourseRecords
        var etrs = new List<ETRCourseRecord>
        {
            new() { ETRCourseRecordId = 1, EnrollmentId = 1, Status = EtrStatus.UnderReview, IsLocked = false, IsDeleted = false },
            new() { ETRCourseRecordId = 2, EnrollmentId = 2, Status = EtrStatus.UnderReview, IsLocked = false, IsDeleted = false },
            new() { ETRCourseRecordId = 3, EnrollmentId = 3, Status = EtrStatus.UnderReview, IsLocked = false, IsDeleted = false },
            new() { ETRCourseRecordId = 4, EnrollmentId = 4, Status = EtrStatus.UnderReview, IsLocked = false, IsDeleted = false }
        };
        var etrRepo = new Mock<IETRCourseRecordRepository>();
        etrRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(etrs);
        uow.Setup(u => u.ETRCourseRecordRepository).Returns(etrRepo.Object);

        // 11. Assessments
        var assessments = new List<Assessment>
        {
            new() { AssessmentId = 1, CourseId = 1, SubjectId = 1, ComponentName = "Final Exam (Theory) - Subject: Aviation Regulations", PassingScore = 70.00m, Weight = 100.00m, IsRequired = true, IsDeleted = false },
            new() { AssessmentId = 2, CourseId = 1, SubjectId = 2, ComponentName = "Aircraft Systems Midterm & Final Exam", PassingScore = 75.00m, Weight = 100.00m, IsRequired = true, IsDeleted = false }
        };
        var asmRepo = new Mock<IGenericRepository<Assessment>>();
        asmRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(assessments);
        asmRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(assessments[0]);
        uow.Setup(u => u.AssessmentRepository).Returns(asmRepo.Object);

        // 12. Sessions
        var sessions = new List<Session>
        {
            new() { SessionId = 1, ClassId = 1, SubjectId = 1, SessionTitle = "Flight Operations Briefing - AMT-101-C1", SessionDate = new DateTime(2026, 10, 15, 8, 0, 0), IsConfirmed = false, TrainingType = TrainingType.Theory, LessonCode = "AMT-SES-01", IsDeleted = false },
            new() { SessionId = 2, ClassId = 1, SubjectId = 1, SessionTitle = "Aviation Law & CAAV Flight Standards", SessionDate = new DateTime(2026, 9, 10, 8, 0, 0), IsConfirmed = true, TrainingType = TrainingType.Theory, LessonCode = "AMT-SES-02", IsDeleted = false }
        };
        var sesRepo = new Mock<IGenericRepository<Session>>();
        sesRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sessions);
        sesRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(sessions[0]);
        uow.Setup(u => u.SessionRepository).Returns(sesRepo.Object);

        // 13. SubjectResults
        var subjectResults = new List<SubjectResult>
        {
            new() { SubjectResultId = 1, EtrId = 1, CourseId = 1, SubjectId = 1, AttendanceRate = 100.00m, Score = 85.00m, Status = SubjectResultStatus.Pending, IsDeleted = false },
            new() { SubjectResultId = 2, EtrId = 2, CourseId = 1, SubjectId = 1, AttendanceRate = 100.00m, Score = 90.00m, Status = SubjectResultStatus.Pending, IsDeleted = false },
            new() { SubjectResultId = 3, EtrId = 3, CourseId = 1, SubjectId = 1, AttendanceRate = 100.00m, Score = 72.00m, Status = SubjectResultStatus.Pending, IsDeleted = false },
            new() { SubjectResultId = 4, EtrId = 4, CourseId = 1, SubjectId = 1, AttendanceRate = 80.00m, Score = 80.00m, Status = SubjectResultStatus.Pending, IsDeleted = false }
        };
        var srRepo = new Mock<IGenericRepository<SubjectResult>>();
        srRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(subjectResults);
        uow.Setup(u => u.SubjectResultRepository).Returns(srRepo.Object);

        return (uow, clsSvc, enrSvc);
    }

    private static string ReportDir => Path.GetFullPath(@"..\..\..\..\Report");

    [Fact]
    public async Task Validate_Demo_Bulk_Create_Accounts_ShouldPass100Percent()
    {
        var filePath = Path.Combine(ReportDir, "Demo_Bulk_Create_Accounts.xlsx");
        Assert.True(File.Exists(filePath), $"File not found: {filePath}");

        var (uow, clsSvc, enrSvc) = BuildSeededMocks();
        var service = new ImportService(uow.Object, clsSvc.Object, enrSvc.Object);

        using var fs = File.OpenRead(filePath);
        var result = await service.ValidateAccountImportAsync(fs, isCallerAdmin: true);

        foreach (var err in result.Errors)
        {
            _output.WriteLine($"[Account Import Error] Row {err.Row} Col {err.Column}: {err.Message}");
        }

        Assert.Empty(result.Errors);
        Assert.True(result.CanCommit);
        Assert.Equal(5, result.TotalRows);
        Assert.Equal(5, result.ValidRows);
        Assert.Equal(0, result.ErrorRows);
    }

    [Fact]
    public async Task Validate_Demo_Bulk_Create_Students_ShouldPass100Percent()
    {
        var filePath = Path.Combine(ReportDir, "Demo_Bulk_Create_Students.xlsx");
        Assert.True(File.Exists(filePath), $"File not found: {filePath}");

        var (uow, clsSvc, enrSvc) = BuildSeededMocks();
        var service = new ImportService(uow.Object, clsSvc.Object, enrSvc.Object);

        using var fs = File.OpenRead(filePath);
        var result = await service.ValidateStudentImportAsync(fs);

        foreach (var err in result.Errors)
        {
            _output.WriteLine($"[Student Import Error] Row {err.Row} Col {err.Column}: {err.Message}");
        }

        Assert.Empty(result.Errors);
        Assert.True(result.CanCommit);
        Assert.Equal(5, result.TotalRows);
        Assert.Equal(5, result.ValidRows);
        Assert.Equal(0, result.ErrorRows);
    }

    [Fact]
    public async Task Validate_Demo_Bulk_Import_Classes_Roster_ShouldPass100Percent()
    {
        var filePath = Path.Combine(ReportDir, "Demo_Bulk_Import_Classes_Roster.xlsx");
        Assert.True(File.Exists(filePath), $"File not found: {filePath}");

        var (uow, clsSvc, enrSvc) = BuildSeededMocks();
        var service = new ImportService(uow.Object, clsSvc.Object, enrSvc.Object);

        using var fs = File.OpenRead(filePath);
        var result = await service.ValidateClassRosterImportAsync(fs);

        foreach (var err in result.Errors)
        {
            _output.WriteLine($"[Class Roster Import Error] Row {err.Row} Col {err.Column}: {err.Message}");
        }

        Assert.Empty(result.Errors);
        Assert.True(result.CanCommit);
        Assert.True(result.TotalRows > 0);
        Assert.Equal(0, result.ErrorRows);
    }

    [Fact]
    public async Task Validate_Demo_Attendance_Import_Session_ShouldPass100Percent()
    {
        var filePath = Path.Combine(ReportDir, "Demo_Attendance_Import_Session.xlsx");
        Assert.True(File.Exists(filePath), $"File not found: {filePath}");

        var (uow, clsSvc, enrSvc) = BuildSeededMocks();
        var service = new ImportService(uow.Object, clsSvc.Object, enrSvc.Object);

        using var fs = File.OpenRead(filePath);
        var result = await service.ValidateAttendanceImportAsync(sessionId: 1, fs);

        foreach (var err in result.Errors)
        {
            _output.WriteLine($"[Attendance Import Error] Row {err.Row} Col {err.Column}: {err.Message}");
        }

        Assert.Empty(result.Errors);
        Assert.True(result.CanCommit);
        Assert.Equal(4, result.TotalRows);
        Assert.Equal(4, result.ValidRows);
        Assert.Equal(0, result.ErrorRows);
    }

    [Fact]
    public async Task Validate_Demo_Assessment_Import_Scores_ShouldPass100Percent()
    {
        var filePath = Path.Combine(ReportDir, "Demo_Assessment_Import_Scores.xlsx");
        Assert.True(File.Exists(filePath), $"File not found: {filePath}");

        var (uow, clsSvc, enrSvc) = BuildSeededMocks();
        var service = new ImportService(uow.Object, clsSvc.Object, enrSvc.Object);

        using var fs = File.OpenRead(filePath);
        var result = await service.ValidateAssessmentImportAsync(assessmentId: 1, fs);

        foreach (var err in result.Errors)
        {
            _output.WriteLine($"[Assessment Import Error] Row {err.Row} Col {err.Column}: {err.Message}");
        }

        Assert.Empty(result.Errors);
        Assert.True(result.CanCommit);
        Assert.Equal(4, result.TotalRows);
        Assert.Equal(4, result.ValidRows);
        Assert.Equal(0, result.ErrorRows);
    }
}
