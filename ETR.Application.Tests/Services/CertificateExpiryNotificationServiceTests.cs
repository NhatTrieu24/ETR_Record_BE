using ETR.Application.DTOs.Email;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace ETR.Application.Tests.Services;

public class CertificateExpiryNotificationServiceTests
{
    private static readonly DateTime Now = new(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc);

    private static Mock<IUnitOfWork> BuildUnitOfWork(
        List<CourseEnrollment> enrollments, List<Class> classes, List<ETRCourseRecord> etrs,
        List<Account> accounts, List<UserProfile> profiles, List<Course> courses)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CourseEnrollmentRepository.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(enrollments);
        unitOfWork.Setup(u => u.ClassRepository.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(classes);
        unitOfWork.Setup(u => u.ETRCourseRecordRepository.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(etrs);
        unitOfWork.Setup(u => u.AccountRepository.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(accounts);
        unitOfWork.Setup(u => u.UserProfileRepository.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(profiles);
        unitOfWork.Setup(u => u.CourseRepository.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(courses);
        return unitOfWork;
    }

    private static (List<CourseEnrollment>, List<Class>, List<ETRCourseRecord>, List<Account>, List<UserProfile>, List<Course>) OneExpiringCandidate(int daysUntilExpiry, string email = "student@example.com")
    {
        var enrollments = new List<CourseEnrollment> { new() { EnrollmentId = 1, AccountId = 10, ClassId = 100 } };
        var classes = new List<Class> { new() { ClassId = 100, CourseId = 5 } };
        var etrs = new List<ETRCourseRecord>
        {
            new() { ETRCourseRecordId = 1, EnrollmentId = 1, Status = EtrStatus.Completed, ExpiryDate = Now.AddDays(daysUntilExpiry) }
        };
        var accounts = new List<Account> { new() { AccountId = 10, Username = email } };
        var profiles = new List<UserProfile> { new() { AccountId = 10, FullName = "Nguyễn Văn A", Email = email } };
        var courses = new List<Course> { new() { CourseId = 5, CourseName = "An toàn lao động" } };

        return (enrollments, classes, etrs, accounts, profiles, courses);
    }

    [Fact]
    public async Task SendExpiryRemindersAsync_SendsTemplatedEmail_ForMatchingCandidate()
    {
        var (enrollments, classes, etrs, accounts, profiles, courses) = OneExpiringCandidate(7);
        var unitOfWork = BuildUnitOfWork(enrollments, classes, etrs, accounts, profiles, courses);
        var emailService = new Mock<IEmailService>();
        var logger = Mock.Of<ILogger<CertificateExpiryNotificationService>>();

        var service = new CertificateExpiryNotificationService(unitOfWork.Object, emailService.Object, logger, () => Now);
        var sentCount = await service.SendExpiryRemindersAsync(CancellationToken.None);

        Assert.Equal(1, sentCount);
        emailService.Verify(e => e.SendTemplatedEmailAsync(
            "student@example.com",
            "Nguyễn Văn A",
            "CertificateExpiryReminder.html",
            It.IsAny<string>(),
            It.Is<IReadOnlyDictionary<string, string>>(t =>
                t["FullName"] == "Nguyễn Văn A" &&
                t["CourseName"] == "An toàn lao động" &&
                t["DaysRemaining"] == "7"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendExpiryRemindersAsync_Skips_WhenProfileHasNoEmail()
    {
        var (enrollments, classes, etrs, accounts, profiles, courses) = OneExpiringCandidate(3, email: "");
        var unitOfWork = BuildUnitOfWork(enrollments, classes, etrs, accounts, profiles, courses);
        var emailService = new Mock<IEmailService>();
        var logger = Mock.Of<ILogger<CertificateExpiryNotificationService>>();

        var service = new CertificateExpiryNotificationService(unitOfWork.Object, emailService.Object, logger, () => Now);
        var sentCount = await service.SendExpiryRemindersAsync(CancellationToken.None);

        Assert.Equal(0, sentCount);
        emailService.Verify(e => e.SendTemplatedEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendExpiryRemindersAsync_ReturnsZero_WhenNoCandidateMatches()
    {
        var (enrollments, classes, etrs, accounts, profiles, courses) = OneExpiringCandidate(15);
        var unitOfWork = BuildUnitOfWork(enrollments, classes, etrs, accounts, profiles, courses);
        var emailService = new Mock<IEmailService>();
        var logger = Mock.Of<ILogger<CertificateExpiryNotificationService>>();

        var service = new CertificateExpiryNotificationService(unitOfWork.Object, emailService.Object, logger, () => Now);
        var sentCount = await service.SendExpiryRemindersAsync(CancellationToken.None);

        Assert.Equal(0, sentCount);
    }

    [Fact]
    public async Task SendExpiryRemindersAsync_ContinuesToNextCandidate_WhenOneEmailThrows()
    {
        var enrollments = new List<CourseEnrollment>
        {
            new() { EnrollmentId = 1, AccountId = 10, ClassId = 100 },
            new() { EnrollmentId = 2, AccountId = 20, ClassId = 100 }
        };
        var classes = new List<Class> { new() { ClassId = 100, CourseId = 5 } };
        var etrs = new List<ETRCourseRecord>
        {
            new() { ETRCourseRecordId = 1, EnrollmentId = 1, Status = EtrStatus.Completed, ExpiryDate = Now.AddDays(30) },
            new() { ETRCourseRecordId = 2, EnrollmentId = 2, Status = EtrStatus.Completed, ExpiryDate = Now.AddDays(30) }
        };
        var accounts = new List<Account>
        {
            new() { AccountId = 10, Username = "a@example.com" },
            new() { AccountId = 20, Username = "b@example.com" }
        };
        var profiles = new List<UserProfile>
        {
            new() { AccountId = 10, FullName = "A", Email = "a@example.com" },
            new() { AccountId = 20, FullName = "B", Email = "b@example.com" }
        };
        var courses = new List<Course> { new() { CourseId = 5, CourseName = "An toàn lao động" } };

        var unitOfWork = BuildUnitOfWork(enrollments, classes, etrs, accounts, profiles, courses);
        var emailService = new Mock<IEmailService>();
        emailService.Setup(e => e.SendTemplatedEmailAsync(
                "a@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));
        emailService.Setup(e => e.SendTemplatedEmailAsync(
                "b@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var logger = Mock.Of<ILogger<CertificateExpiryNotificationService>>();

        var service = new CertificateExpiryNotificationService(unitOfWork.Object, emailService.Object, logger, () => Now);
        var sentCount = await service.SendExpiryRemindersAsync(CancellationToken.None);

        Assert.Equal(1, sentCount);
    }
}
