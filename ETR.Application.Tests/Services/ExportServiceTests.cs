using ClosedXML.Excel;
using ETR.Application.Services;
using ETR.Domain.Entities;

namespace ETR.Application.Tests.Services;

public class ExportServiceTests
{
    private static (ETRCourseRecord Etr, CourseEnrollment Enrollment, Class Class, Course Course, UserProfile Student, Dictionary<int, Subject> Subjects) BuildSampleEtr()
    {
        var etr = new ETRCourseRecord
        {
            ETRCourseRecordId = 42,
            Status = "Completed",
            SubmittedAt = new DateTime(2026, 5, 2),
            VerifiedAt = new DateTime(2026, 5, 5),
            CompletedAt = new DateTime(2026, 5, 10),
            SubjectResults = new List<SubjectResult>
            {
                new() { SubjectId = 1, Status = "Passed", Score = 85.5m, AttendanceRate = 100m },
                new() { SubjectId = 2, Status = "Failed", Score = 40m, AttendanceRate = 70m }
            }
        };
        var enrollment = new CourseEnrollment { EnrollmentId = 1, AccountId = 12, ClassId = 1 };
        var trainingClass = new Class { ClassId = 1, ClassCode = "AMT101-C1", ClassName = "AMT Batch 1", CourseId = 1 };
        var course = new Course { CourseId = 1, CourseCode = "AMT-101", CourseName = "Aircraft Maintenance" };
        var studentProfile = new UserProfile { AccountId = 12, UserCode = "STU-01", FullName = "Jane Student" };
        var subjects = new Dictionary<int, Subject>
        {
            [1] = new Subject { SubjectId = 1, SubjectCode = "SUB-CS", SubjectName = "C# Fundamentals" },
            [2] = new Subject { SubjectId = 2, SubjectCode = "SUB-EF", SubjectName = "EF Core" }
        };

        return (etr, enrollment, trainingClass, course, studentProfile, subjects);
    }

    [Fact]
    public void BuildEtrSummaryExcel_WhenGivenEtrData_ExpectsWorksheetWithHeaderAndSubjectResults()
    {
        var (etr, enrollment, trainingClass, course, studentProfile, subjects) = BuildSampleEtr();

        var bytes = ExportService.BuildEtrSummaryExcel(etr, enrollment, trainingClass, course, studentProfile, subjects);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet("ETR Summary");
        var cellValues = sheet.CellsUsed().Select(c => c.GetString()).ToList();

        Assert.Contains("STU-01", cellValues);
        Assert.Contains("Jane Student", cellValues);
        Assert.Contains("AMT101-C1", cellValues);
        Assert.Contains("SUB-CS", cellValues);
        Assert.Contains("SUB-EF", cellValues);
        Assert.Contains("Passed", cellValues);
        Assert.Contains("Failed", cellValues);
    }

    [Fact]
    public void BuildEtrSummaryExcel_WhenStudentProfileIsNull_ExpectsUnknownPlaceholderInsteadOfCrashing()
    {
        var (etr, enrollment, trainingClass, course, _, subjects) = BuildSampleEtr();

        var bytes = ExportService.BuildEtrSummaryExcel(etr, enrollment, trainingClass, course, studentProfile: null, subjects);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet("ETR Summary");
        var cellValues = sheet.CellsUsed().Select(c => c.GetString()).ToList();

        Assert.Contains("(unknown)", cellValues);
    }

    [Fact]
    public void BuildTrainingPackageZipFileName_WhenCodesAreClean_ExpectsExactFormat()
    {
        var result = ExportService.BuildTrainingPackageZipFileName("STU-01", "AMT101-C1");

        Assert.Equal("STU-01_AMT101-C1_Training_Package.zip", result);
    }

    [Fact]
    public void BuildTrainingPackageZipFileName_WhenCodesContainInvalidFileNameChars_ExpectsSanitized()
    {
        // Codes here contain characters unsafe on Windows (: * ? " ...) even though this test
        // may run on macOS/Linux, where they'd otherwise be treated as ordinary characters —
        // the output must stay portable to whatever OS eventually opens the file.
        var result = ExportService.BuildTrainingPackageZipFileName("STU/01", "AMT:101*C1");

        Assert.Equal("STU_01_AMT_101_C1_Training_Package.zip", result);
        Assert.DoesNotContain('/', result);
        Assert.DoesNotContain(':', result);
        Assert.DoesNotContain('*', result);
    }

    [Theory]
    [InlineData(null, "AMT101-C1")]
    [InlineData("STU-01", null)]
    [InlineData("", "AMT101-C1")]
    [InlineData("STU-01", "")]
    public void BuildTrainingPackageZipFileName_WhenEitherCodeIsMissing_ExpectsArgumentException(string? studentCode, string? classCode)
    {
        Assert.Throws<ArgumentException>(() => ExportService.BuildTrainingPackageZipFileName(studentCode!, classCode!));
    }
}
