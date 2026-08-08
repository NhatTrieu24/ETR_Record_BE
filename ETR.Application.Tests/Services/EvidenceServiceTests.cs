using ETR.Application.Compliance;
using ETR.Application.DTOs.Evidence.Requests;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;

namespace ETR.Application.Tests.Services;

public class EvidenceServiceTests
{
    private static (EvidenceService Service, Mock<IUnitOfWork> UnitOfWork, Mock<IAuditLogRepository> AuditLogRepo) BuildService(params EvidenceFile[] evidences)
    {
        var evidenceRepo = new Mock<IGenericRepository<EvidenceFile>>();
        foreach (var evidence in evidences)
        {
            evidenceRepo.Setup(r => r.GetByIdAsync(evidence.EvidenceFileId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(evidence);
        }

        var auditLogRepo = new Mock<IAuditLogRepository>();

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(u => u.EvidenceFileRepository).Returns(evidenceRepo.Object);
        unitOfWork.SetupGet(u => u.AuditLogRepository).Returns(auditLogRepo.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return (new EvidenceService(unitOfWork.Object), unitOfWork, auditLogRepo);
    }

    [Fact]
    public async Task VerifyEvidenceAsync_WhenVerifierUploadedTheEvidence_ExpectsForbiddenAccessException()
    {
        var evidence = new EvidenceFile { EvidenceFileId = 1, UploadedByAccountId = 42, VerificationStatus = "Pending" };
        var (service, _, _) = BuildService(evidence);
        var request = new VerifyEvidenceRequest { VerificationStatus = "Verified" };

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.VerifyEvidenceAsync(1, request, verifiedByAccountId: 42, CancellationToken.None));
    }

    [Fact]
    public async Task VerifyEvidenceAsync_WhenVerifierDidNotUploadTheEvidence_ExpectsSuccess()
    {
        var evidence = new EvidenceFile { EvidenceFileId = 1, UploadedByAccountId = 42, VerificationStatus = "Pending" };
        var (service, _, _) = BuildService(evidence);
        var request = new VerifyEvidenceRequest { VerificationStatus = "Verified" };

        var response = await service.VerifyEvidenceAsync(1, request, verifiedByAccountId: 99, CancellationToken.None);

        Assert.Equal("Verified", response.VerificationStatus);
        Assert.Equal(99, response.VerifiedByAccountId);
    }

    [Fact]
    public async Task VerifyEvidenceAsync_WhenVerified_WritesOneAuditLogEntry()
    {
        var evidence = new EvidenceFile { EvidenceFileId = 1, UploadedByAccountId = 42, VerificationStatus = "Pending" };
        var (service, _, auditLogRepo) = BuildService(evidence);
        var request = new VerifyEvidenceRequest { VerificationStatus = "Verified" };

        await service.VerifyEvidenceAsync(1, request, verifiedByAccountId: 99, CancellationToken.None);

        auditLogRepo.Verify(r => r.AddAsync(It.Is<AuditLog>(a => a.RecordId == 1 && a.NewValue == "Verified"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkVerifyEvidencesAsync_AllValid_VerifiesEveryItemAndWritesOneAuditLogPerItem()
    {
        var evidences = new[]
        {
            new EvidenceFile { EvidenceFileId = 1, UploadedByAccountId = 42, VerificationStatus = "Pending" },
            new EvidenceFile { EvidenceFileId = 2, UploadedByAccountId = 42, VerificationStatus = "Pending" },
            new EvidenceFile { EvidenceFileId = 3, UploadedByAccountId = 42, VerificationStatus = "Pending" },
        };
        var (service, _, auditLogRepo) = BuildService(evidences);
        var request = new BulkVerifyEvidenceRequest { EvidenceIds = new List<int> { 1, 2, 3 }, VerificationStatus = "Verified" };

        var result = await service.BulkVerifyEvidencesAsync(request, verifiedByAccountId: 99, CancellationToken.None);

        Assert.Equal(3, result.Verified.Count);
        Assert.Empty(result.Failed);
        Assert.All(evidences, e => Assert.Equal("Verified", e.VerificationStatus));
        auditLogRepo.Verify(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task BulkVerifyEvidencesAsync_OneItemSelfUploadedAndOneMissing_StillVerifiesTheRest()
    {
        var evidences = new[]
        {
            new EvidenceFile { EvidenceFileId = 1, UploadedByAccountId = 42, VerificationStatus = "Pending" },
            new EvidenceFile { EvidenceFileId = 2, UploadedByAccountId = 99, VerificationStatus = "Pending" }, // uploaded by the verifier themselves
        };
        var (service, _, auditLogRepo) = BuildService(evidences);
        var request = new BulkVerifyEvidenceRequest { EvidenceIds = new List<int> { 1, 2, 999 }, VerificationStatus = "Verified" };

        var result = await service.BulkVerifyEvidencesAsync(request, verifiedByAccountId: 99, CancellationToken.None);

        Assert.Single(result.Verified);
        Assert.Equal(1, result.Verified[0].EvidenceFileId);
        Assert.Equal(2, result.Failed.Count);
        Assert.Contains(result.Failed, f => f.EvidenceFileId == 2);
        Assert.Contains(result.Failed, f => f.EvidenceFileId == 999);
        auditLogRepo.Verify(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkVerifyEvidencesAsync_RejectedWithoutComment_ThrowsValidationExceptionBeforeTouchingAnyItem()
    {
        var evidences = new[] { new EvidenceFile { EvidenceFileId = 1, UploadedByAccountId = 42, VerificationStatus = "Pending" } };
        var (service, _, auditLogRepo) = BuildService(evidences);
        var request = new BulkVerifyEvidenceRequest { EvidenceIds = new List<int> { 1 }, VerificationStatus = "Rejected", VerificationComment = null };

        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
            () => service.BulkVerifyEvidencesAsync(request, verifiedByAccountId: 99, CancellationToken.None));

        Assert.Equal("Pending", evidences[0].VerificationStatus);
        auditLogRepo.Verify(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadEvidenceAsync_InstructorNotAssignedToClass_ThrowsForbidden()
    {
        // "Sân nhà ai nấy đá" (team decision 2026-08-08, docs/todo/addition.md).
        var subjectResult = new SubjectResult { SubjectResultId = 1, EtrId = 1 };
        var etr = new ETRCourseRecord { ETRCourseRecordId = 1, EnrollmentId = 1 };
        var enrollment = new CourseEnrollment { EnrollmentId = 1, ClassId = 10 };
        var trainingClass = new Class { ClassId = 10, InstructorAccountId = 99 };

        var unitOfWork = new Mock<IUnitOfWork>();

        var subjectResultRepo = new Mock<IGenericRepository<SubjectResult>>();
        subjectResultRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(subjectResult);
        unitOfWork.SetupGet(u => u.SubjectResultRepository).Returns(subjectResultRepo.Object);

        var etrRepo = new Mock<IETRCourseRecordRepository>();
        etrRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(etr);
        unitOfWork.SetupGet(u => u.ETRCourseRecordRepository).Returns(etrRepo.Object);

        var enrollmentRepo = new Mock<IGenericRepository<CourseEnrollment>>();
        enrollmentRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(enrollment);
        unitOfWork.SetupGet(u => u.CourseEnrollmentRepository).Returns(enrollmentRepo.Object);

        var classRepo = new Mock<IGenericRepository<Class>>();
        classRepo.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(trainingClass);
        unitOfWork.SetupGet(u => u.ClassRepository).Returns(classRepo.Object);

        var service = new EvidenceService(unitOfWork.Object);

        var file = new Mock<IFormFile>();
        file.SetupGet(f => f.Length).Returns(100);
        file.SetupGet(f => f.FileName).Returns("evidence.jpg");
        file.SetupGet(f => f.ContentType).Returns("image/jpeg");

        var request = new UploadEvidenceRequest { EvidenceTypeId = 1, AccountId = 100, SubjectResultId = 1, File = file.Object };

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.UploadEvidenceAsync(request, uploadedByAccountId: 42, uploadedByRoleName: "Instructor", webRootPath: "/tmp", CancellationToken.None));
    }
}
