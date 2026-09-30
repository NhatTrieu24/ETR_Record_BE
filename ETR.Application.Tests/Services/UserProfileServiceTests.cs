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

        var request = new VerifyPilotCredentialsRequest(true, "CAAV License Verified", VerificationMethod: "DirectAdminInspection");

        // Act
        var result = await service.VerifyPilotCredentialsAsync(10, request, verifiedByAccountId: 1);

        // Assert
        Assert.True(result.IsCredentialsVerified);
        Assert.Equal(1, result.CredentialsVerifiedByAccountId);
        Assert.NotNull(result.CredentialsVerifiedAt);
        Assert.Single(auditLogs);
        Assert.Contains("VERIFIED", auditLogs[0].Description);
        Assert.Contains("CAAV License Verified", auditLogs[0].NewValue);
        Assert.Contains("DirectAdminInspection", auditLogs[0].NewValue);
    }

    [Fact]
    public async Task UpdatePilotCredentialsAsync_AdminUpdate_WithSubstantiveChange_ResetsVerificationStatusAndClearsVerifier()
    {
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(c => c.AccountId).Returns(1);
        currentUserService.Setup(c => c.RoleName).Returns("Admin");

        var profile = new UserProfile
        {
            AccountId = 10,
            UserCode = "STU-010",
            FullName = "Nguyen Pilot",
            Email = "pilot@etr.com",
            LicenseType = "PPL",
            LicenseNumber = "VN-OLD",
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
            "CPL", "VN-NEW-999", DateTime.UtcNow.AddYears(2),
            "Class 1", DateTime.UtcNow.AddMonths(12),
            5, DateTime.UtcNow.AddYears(6), "A320");

        // Act: Admin updates the credentials with new License Type & Number
        var result = await service.UpdatePilotCredentialsAsync(10, request, updatedByAccountId: 1, isSelfUpdate: false);

        // Assert: Verification status MUST be reset to false and verifier info cleared
        Assert.Equal("CPL", result.LicenseType);
        Assert.Equal("VN-NEW-999", result.LicenseNumber);
        Assert.False(result.IsCredentialsVerified);
        Assert.Null(result.CredentialsVerifiedByAccountId);
        Assert.Null(result.CredentialsVerifiedAt);
    }

    [Fact]
    public async Task UpdatePilotCredentialsAsync_NoSubstantiveChange_PreservesVerificationStatusAndVerifier()
    {
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        var verifiedDate = DateTime.UtcNow.AddDays(-5);

        var profile = new UserProfile
        {
            AccountId = 10,
            UserCode = "STU-010",
            FullName = "Nguyen Pilot",
            Email = "pilot@etr.com",
            LicenseType = "CPL",
            LicenseNumber = "VN-12345",
            MedicalClass = "Class 1",
            IcaoElpLevel = 5,
            TypeRatings = "A320",
            IsCredentialsVerified = true,
            CredentialsVerifiedByAccountId = 2,
            CredentialsVerifiedAt = verifiedDate
        };

        var profileRepo = new Mock<IGenericRepository<UserProfile>>();
        profileRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { profile });
        uow.Setup(u => u.UserProfileRepository).Returns(profileRepo.Object);

        var auditRepo = new Mock<IAuditLogRepository>();
        uow.Setup(u => u.AuditLogRepository).Returns(auditRepo.Object);

        var service = new UserProfileService(uow.Object, currentUserService.Object);

        // Submitting identical credentials
        var request = new UpdatePilotCredentialsRequest(
            "CPL", "VN-12345", null,
            "Class 1", null,
            5, null, "A320");

        // Act
        var result = await service.UpdatePilotCredentialsAsync(10, request, updatedByAccountId: 10, isSelfUpdate: true);

        // Assert: Verification status remains true because nothing changed
        Assert.True(result.IsCredentialsVerified);
        Assert.Equal(2, result.CredentialsVerifiedByAccountId);
        Assert.Equal(verifiedDate, result.CredentialsVerifiedAt);
    }

    [Fact]
    public async Task VerifyPilotCredentialsAsync_OfflineVerification_RecordsOfflineBasisInAudit()
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

        var attachRepo = new Mock<IGenericRepository<Attachment>>();
        attachRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Attachment>()); // No attachments on server
        uow.Setup(u => u.AttachmentRepository).Returns(attachRepo.Object);

        var auditLogs = new List<AuditLog>();
        var auditRepo = new Mock<IAuditLogRepository>();
        auditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((a, _) => auditLogs.Add(a))
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.AuditLogRepository).Returns(auditRepo.Object);

        var service = new UserProfileService(uow.Object, currentUserService.Object);

        var request = new VerifyPilotCredentialsRequest(
            IsVerified: true,
            Comment: "Inspected physical CAAV certificate card directly at flight ops",
            VerificationMethod: "PhysicalCardInspection");

        // Act
        var result = await service.VerifyPilotCredentialsAsync(10, request, verifiedByAccountId: 1);

        // Assert
        Assert.True(result.IsCredentialsVerified);
        Assert.Equal(1, result.CredentialsVerifiedByAccountId);
        Assert.Single(auditLogs);
        Assert.Contains("PhysicalCardInspection", auditLogs[0].NewValue);
        Assert.Contains("Offline/Direct check without uploaded attachments", auditLogs[0].NewValue);
    }

    [Fact]
    public async Task VerifyPilotCredentialsAsync_DocumentReview_RecordsReviewedAttachmentsInAudit()
    {
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(c => c.AccountId).Returns(1);
        currentUserService.Setup(c => c.RoleName).Returns("Academic");

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

        var attachments = new List<Attachment>
        {
            new() { AttachmentId = 101, OwnerType = "UserProfile", OwnerId = 10, DocType = "License", FileName = "cpl_license.pdf" },
            new() { AttachmentId = 102, OwnerType = "UserProfile", OwnerId = 10, DocType = "Medical", FileName = "class1_med.pdf" }
        };
        var attachRepo = new Mock<IGenericRepository<Attachment>>();
        attachRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachments);
        uow.Setup(u => u.AttachmentRepository).Returns(attachRepo.Object);

        var auditLogs = new List<AuditLog>();
        var auditRepo = new Mock<IAuditLogRepository>();
        auditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((a, _) => auditLogs.Add(a))
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.AuditLogRepository).Returns(auditRepo.Object);

        var service = new UserProfileService(uow.Object, currentUserService.Object);

        var request = new VerifyPilotCredentialsRequest(
            IsVerified: true,
            Comment: "Approved based on uploaded scans",
            ReviewedAttachmentIds: new List<int> { 101, 102 });

        // Act
        var result = await service.VerifyPilotCredentialsAsync(10, request, verifiedByAccountId: 1);

        // Assert
        Assert.True(result.IsCredentialsVerified);
        Assert.Single(auditLogs);
        Assert.Contains("License:cpl_license.pdf", auditLogs[0].NewValue);
        Assert.Contains("Medical:class1_med.pdf", auditLogs[0].NewValue);
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

    [Fact]
    public async Task VerifyPilotCredentialsAsync_WrongOwnerAttachmentId_ThrowsBusinessRuleViolationException()
    {
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(c => c.AccountId).Returns(1);
        currentUserService.Setup(c => c.RoleName).Returns("Academic");

        var profile = new UserProfile { AccountId = 10, FullName = "Nguyen Pilot", Email = "pilot@etr.com" };
        var profileRepo = new Mock<IGenericRepository<UserProfile>>();
        profileRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { profile });
        uow.Setup(u => u.UserProfileRepository).Returns(profileRepo.Object);

        // Attachment belongs to Student 99, not Student 10
        var attachments = new List<Attachment>
        {
            new() { AttachmentId = 999, OwnerType = "UserProfile", OwnerId = 99, DocType = "License", FileName = "other_license.pdf" }
        };
        var attachRepo = new Mock<IGenericRepository<Attachment>>();
        attachRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachments);
        uow.Setup(u => u.AttachmentRepository).Returns(attachRepo.Object);

        var service = new UserProfileService(uow.Object, currentUserService.Object);

        var request = new VerifyPilotCredentialsRequest(
            IsVerified: true,
            ReviewedAttachmentIds: new List<int> { 999 });

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            service.VerifyPilotCredentialsAsync(10, request, verifiedByAccountId: 1));

        Assert.Contains("#999", ex.Message);
        Assert.Contains("không thuộc hồ sơ của học viên #10", ex.Message);
    }

    [Fact]
    public async Task VerifyPilotCredentialsAsync_NonExistentOrDeletedAttachmentId_ThrowsBusinessRuleViolationException()
    {
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(c => c.AccountId).Returns(1);
        currentUserService.Setup(c => c.RoleName).Returns("Admin");

        var profile = new UserProfile { AccountId = 10, FullName = "Nguyen Pilot", Email = "pilot@etr.com" };
        var profileRepo = new Mock<IGenericRepository<UserProfile>>();
        profileRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { profile });
        uow.Setup(u => u.UserProfileRepository).Returns(profileRepo.Object);

        // Attachment 101 is soft-deleted
        var attachments = new List<Attachment>
        {
            new() { AttachmentId = 101, OwnerType = "UserProfile", OwnerId = 10, DocType = "License", FileName = "cpl.pdf", IsDeleted = true }
        };
        var attachRepo = new Mock<IGenericRepository<Attachment>>();
        attachRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachments);
        uow.Setup(u => u.AttachmentRepository).Returns(attachRepo.Object);

        var service = new UserProfileService(uow.Object, currentUserService.Object);

        var request = new VerifyPilotCredentialsRequest(
            IsVerified: true,
            ReviewedAttachmentIds: new List<int> { 101 }); // Deleted

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            service.VerifyPilotCredentialsAsync(10, request, verifiedByAccountId: 1));

        Assert.Contains("#101", ex.Message);
        Assert.Contains("không tồn tại, đã bị xóa", ex.Message);
    }

    [Fact]
    public async Task VerifyPilotCredentialsAsync_OfflineWithoutVerificationMethod_ThrowsBusinessRuleViolationException()
    {
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(c => c.AccountId).Returns(1);
        currentUserService.Setup(c => c.RoleName).Returns("Admin");

        var profile = new UserProfile { AccountId = 10, FullName = "Nguyen Pilot", Email = "pilot@etr.com" };
        var profileRepo = new Mock<IGenericRepository<UserProfile>>();
        profileRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { profile });
        uow.Setup(u => u.UserProfileRepository).Returns(profileRepo.Object);

        var service = new UserProfileService(uow.Object, currentUserService.Object);

        // Verification without attachments and without VerificationMethod
        var request = new VerifyPilotCredentialsRequest(
            IsVerified: true,
            Comment: "Direct verification",
            VerificationMethod: "   "); // Empty

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            service.VerifyPilotCredentialsAsync(10, request, verifiedByAccountId: 1));

        Assert.Contains("VerificationMethod", ex.Message);
    }

    [Fact]
    public async Task DeleteCredentialAttachmentAsync_WhenCredentialsVerified_RevokesVerificationStatusAndLogsAudit()
    {
        var uow = new Mock<IUnitOfWork>();
        var currentUserService = new Mock<ICurrentUserService>();

        var profile = new UserProfile
        {
            AccountId = 10,
            FullName = "Nguyen Pilot",
            Email = "pilot@etr.com",
            IsCredentialsVerified = true,
            CredentialsVerifiedByAccountId = 1,
            CredentialsVerifiedAt = DateTime.UtcNow.AddDays(-2)
        };
        var profileRepo = new Mock<IGenericRepository<UserProfile>>();
        profileRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { profile });
        uow.Setup(u => u.UserProfileRepository).Returns(profileRepo.Object);

        var attachment = new Attachment
        {
            AttachmentId = 55,
            OwnerType = "UserProfile",
            OwnerId = 10,
            DocType = "Medical",
            FileName = "medical_class1.pdf",
            UploadedByAccountId = 10,
            IsDeleted = false
        };

        var attachRepo = new Mock<IGenericRepository<Attachment>>();
        attachRepo.Setup(r => r.GetByIdAsync(55, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachment);
        uow.Setup(u => u.AttachmentRepository).Returns(attachRepo.Object);

        var auditLogs = new List<AuditLog>();
        var auditRepo = new Mock<IAuditLogRepository>();
        auditRepo.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AuditLog, CancellationToken>((a, _) => auditLogs.Add(a))
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.AuditLogRepository).Returns(auditRepo.Object);

        var service = new UserProfileService(uow.Object, currentUserService.Object);

        // Act: Student deletes their own medical attachment
        await service.DeleteCredentialAttachmentAsync(55, currentAccountId: 10, roleName: "Student");

        // Assert
        Assert.True(attachment.IsDeleted);
        Assert.False(profile.IsCredentialsVerified);
        Assert.Null(profile.CredentialsVerifiedByAccountId);
        Assert.Null(profile.CredentialsVerifiedAt);

        // Two audit logs: 1 for verification revocation, 1 for attachment deletion
        Assert.Equal(2, auditLogs.Count);
        Assert.Contains(auditLogs, l => l.Description.Contains("REVOKED due to deletion of evidence attachment #55"));
        Assert.Contains(auditLogs, l => l.ActionType == AuditActionType.DELETE.ToString());
    }
}
