using ETR.Application.Compliance;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Moq;

namespace ETR.Application.Tests.Compliance;

public class CertificateValidityCalculatorTests
{
    private static Mock<IUnitOfWork> BuildUnitOfWork(
        List<CourseEnrollment> enrollments, List<Class> classes, List<ETRCourseRecord> etrs)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CourseEnrollmentRepository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollments);
        unitOfWork.Setup(u => u.ClassRepository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(classes);
        unitOfWork.Setup(u => u.ETRCourseRecordRepository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(etrs);
        return unitOfWork;
    }

    [Fact]
    public async Task GetCertificatesNearingExpiryAsync_ReturnsCandidate_WhenExpiryMatchesThreshold()
    {
        var now = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc);
        var enrollments = new List<CourseEnrollment> { new() { EnrollmentId = 1, AccountId = 10, ClassId = 100 } };
        var classes = new List<Class> { new() { ClassId = 100, CourseId = 5 } };
        var etrs = new List<ETRCourseRecord>
        {
            new() { ETRCourseRecordId = 1, EnrollmentId = 1, Status = EtrStatus.Completed, ExpiryDate = now.AddDays(7) }
        };

        var result = await CertificateValidityCalculator.GetCertificatesNearingExpiryAsync(
            BuildUnitOfWork(enrollments, classes, etrs).Object, [3, 7, 30], now, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(10, result[0].AccountId);
        Assert.Equal(5, result[0].CourseId);
        Assert.Equal(7, result[0].DaysUntilExpiry);
    }

    [Fact]
    public async Task GetCertificatesNearingExpiryAsync_ReturnsEmpty_WhenExpiryDoesNotMatchAnyThreshold()
    {
        var now = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc);
        var enrollments = new List<CourseEnrollment> { new() { EnrollmentId = 1, AccountId = 10, ClassId = 100 } };
        var classes = new List<Class> { new() { ClassId = 100, CourseId = 5 } };
        var etrs = new List<ETRCourseRecord>
        {
            new() { ETRCourseRecordId = 1, EnrollmentId = 1, Status = EtrStatus.Completed, ExpiryDate = now.AddDays(15) }
        };

        var result = await CertificateValidityCalculator.GetCertificatesNearingExpiryAsync(
            BuildUnitOfWork(enrollments, classes, etrs).Object, [3, 7, 30], now, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCertificatesNearingExpiryAsync_ExcludesRecordsNotCompleted()
    {
        var now = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc);
        var enrollments = new List<CourseEnrollment> { new() { EnrollmentId = 1, AccountId = 10, ClassId = 100 } };
        var classes = new List<Class> { new() { ClassId = 100, CourseId = 5 } };
        var etrs = new List<ETRCourseRecord>
        {
            new() { ETRCourseRecordId = 1, EnrollmentId = 1, Status = EtrStatus.InProgress, ExpiryDate = now.AddDays(3) }
        };

        var result = await CertificateValidityCalculator.GetCertificatesNearingExpiryAsync(
            BuildUnitOfWork(enrollments, classes, etrs).Object, [3, 7, 30], now, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCertificatesNearingExpiryAsync_UsesLatestRecord_WhenMultipleRecordsExistForSameCourse()
    {
        var now = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc);
        var enrollments = new List<CourseEnrollment> { new() { EnrollmentId = 1, AccountId = 10, ClassId = 100 } };
        var classes = new List<Class> { new() { ClassId = 100, CourseId = 5 } };
        var etrs = new List<ETRCourseRecord>
        {
            // Older record — would have matched the 30-day threshold, but is superseded.
            new() { ETRCourseRecordId = 1, EnrollmentId = 1, Status = EtrStatus.Completed, IssuedDate = now.AddDays(-100), ExpiryDate = now.AddDays(30) },
            // Latest re-issued record — matches the 3-day threshold instead.
            new() { ETRCourseRecordId = 2, EnrollmentId = 1, Status = EtrStatus.Completed, IssuedDate = now.AddDays(-1), ExpiryDate = now.AddDays(3) }
        };

        var result = await CertificateValidityCalculator.GetCertificatesNearingExpiryAsync(
            BuildUnitOfWork(enrollments, classes, etrs).Object, [3, 7, 30], now, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(2, result[0].ETRCourseRecordId);
        Assert.Equal(3, result[0].DaysUntilExpiry);
    }
}
