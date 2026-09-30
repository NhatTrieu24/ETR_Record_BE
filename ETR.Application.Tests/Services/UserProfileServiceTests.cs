using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Moq;
using Xunit;

namespace ETR.Application.Tests.Services;

public class UserProfileServiceTests
{
    [Fact]
    public async Task UpdatePilotCredentialsAsync_SelfUpdate_ResetsVerificationFlagToFalse()
    {
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(c => c.AccountId).Returns(10);
        currentUserService.Setup(c => c.RoleName).Returns("Student");

        var profile = new UserProfile
        {
            AccountId = 10,
            UserCode = "STU-010",
            FullName = "Nguyen Pilot",
            Email = "pilot@etr.com",
            IsCredentialsVerified = true,
            CredentialsVerifiedByAccountId = 1,
            CredentialsVerifiedAt = DateTime.UtcNow.AddDays(-10)
        };

        var profileRepo = new Mock<IGenericRepository<UserProfile>>();
        profileRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { profile });
        uow.Setup(u => u.UserProfileRepository).Returns(profileRepo.Object);

        var auditRepo = new Mock<IAuditLogRepository>();
        uow.Setup(u => u.AuditLogRepository).Returns(auditRepo.Object);

        var service = new UserProfileService(uow.Object, currentUserService.Object);

        var request = new UpdatePilotCredentialsRequest(
            "CPL", "VN-12345", DateTime.UtcNow.AddYears(2),
            "Class 1", DateTime.UtcNow.AddMonths(12),
            5, DateTime.UtcNow.AddYears(6), "A320");

        // Act
        var result = await service.UpdatePilotCredentialsAsync(10, request, updatedByAccountId: 10, isSelfUpdate: true);

        // Assert
        Assert.Equal("CPL", result.LicenseType);
        Assert.Equal("VN-12345", result.LicenseNumber);
        Assert.Equal("Class 1", result.MedicalClass);
        Assert.Equal(5, result.IcaoElpLevel);
        Assert.False(result.IsCredentialsVerified);
        Assert.Null(result.CredentialsVerifiedByAccountId);
        Assert.Null(result.CredentialsVerifiedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(-1)]
    public async Task UpdatePilotCredentialsAsync_InvalidIcaoElpLevel_ThrowsBusinessRuleViolationException(int invalidElpLevel)
    {
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();

        var profile = new UserProfile { AccountId = 10, FullName = "Nguyen Pilot", Email = "pilot@etr.com" };
        var profileRepo = new Mock<IGenericRepository<UserProfile>>();
        profileRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { profile });
        uow.Setup(u => u.UserProfileRepository).Returns(profileRepo.Object);

        var service = new UserProfileService(uow.Object, currentUserService.Object);

        var request = new UpdatePilotCredentialsRequest(
            "CPL", "VN-12345", DateTime.UtcNow.AddYears(2),
            "Class 1", DateTime.UtcNow.AddMonths(12),
            invalidElpLevel, null, null);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            service.UpdatePilotCredentialsAsync(10, request, 10, isSelfUpdate: true));

        Assert.Contains("ICAO ELP Level", ex.Message);
    }

    [Fact]
    public async Task VerifyPilotCredentialsAsync_ByAdmin_SetsIsCredentialsVerifiedTrueWithAudit()
    {
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(c => c.AccountId).Returns(1);
        currentUserService.Setup(c => c.RoleName).Returns("Admin");

        var profile = new UserProfile
        {
            AccountId = 10,
            FullName = "Nguyen Pilot",
            Email = "pilot@etr.com",
            LicenseType = "CPL",
            LicenseNumber = "VN-12345",
            IsCredentialsVerified = false
        };

        var profileRepo = new Mock<IGenericRepository<UserProfile>>();
        profileRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { profile });
        uow.Setup(u => u.UserProfileRepository).Returns(profileRepo.Object);

        var auditLogs = new List<AuditLog>();
        var auditRepo = new Mock<IAuditLogRepository>();
        auditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((a, _) => auditLogs.Add(a))
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.AuditLogRepository).Returns(auditRepo.Object);

        var service = new UserProfileService(uow.Object, currentUserService.Object);

        var request = new VerifyPilotCredentialsRequest(true, "CAAV License Verified");

        // Act
        var result = await service.VerifyPilotCredentialsAsync(10, request, verifiedByAccountId: 1);

        // Assert
        Assert.True(result.IsCredentialsVerified);
        Assert.Equal(1, result.CredentialsVerifiedByAccountId);
        Assert.NotNull(result.CredentialsVerifiedAt);
        Assert.Single(auditLogs);
        Assert.Contains("VERIFIED", auditLogs[0].Description);
        Assert.Contains("CAAV License Verified", auditLogs[0].NewValue);
    }

    [Fact]
    public async Task GetCredentialAttachmentsAsync_OtherStudentAccess_ThrowsUnauthorizedAccessException()
    {
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        var service = new UserProfileService(uow.Object, currentUserService.Object);

        // Student 20 tries to read medical/license of Student 10
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetCredentialAttachmentsAsync(10, currentAccountId: 20, roleName: "Student"));
    }

    [Fact]
    public async Task UploadCredentialAttachmentAsync_ValidSelfUpload_CreatesAttachmentAndAudit()
    {
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();

        var attachments = new List<Attachment>();
        var attachRepo = new Mock<IGenericRepository<Attachment>>();
        attachRepo.Setup(r => r.AddAsync(It.IsAny<Attachment>(), It.IsAny<CancellationToken>()))
            .Callback<Attachment, CancellationToken>((a, _) => attachments.Add(a))
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.AttachmentRepository).Returns(attachRepo.Object);

        var auditLogs = new List<AuditLog>();
        var auditRepo = new Mock<IAuditLogRepository>();
        auditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((a, _) => auditLogs.Add(a))
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.AuditLogRepository).Returns(auditRepo.Object);

        var service = new UserProfileService(uow.Object, currentUserService.Object);

        var request = new UploadCredentialAttachmentRequest(
            "Medical",
            "https://res.cloudinary.com/etr/image/upload/v1/medical_cert.pdf",
            "medical_cert.pdf",
            "medical_cert_pub",
            "application/pdf",
            204800);

        // Act
        var result = await service.UploadCredentialAttachmentAsync(10, request, uploadedByAccountId: 10, roleName: "Student");

        // Assert
        Assert.Equal("Medical", result.DocType);
        Assert.Equal("medical_cert.pdf", result.FileName);
        Assert.Single(attachments);
        Assert.Equal("UserProfile", attachments[0].OwnerType);
        Assert.Equal(10, attachments[0].OwnerId);
        Assert.Single(auditLogs);
    }
}
