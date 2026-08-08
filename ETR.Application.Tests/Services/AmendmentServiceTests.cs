using ETR.Application.DTOs.Amendment.Requests;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using Moq;

namespace ETR.Application.Tests.Services;

public class AmendmentServiceTests
{
    private static (AmendmentService Service, Mock<IUnitOfWork> UnitOfWork, Mock<IAuditLogRepository> AuditLogRepo,
        List<AmendmentRequest> Amendments, List<SubjectSignoff> Signoffs, SubjectResult SubjectResult)
        BuildService(ETRCourseRecord? etr, SubjectResult subjectResult, List<SubjectSignoff>? existingSignoffs = null, List<AmendmentRequest>? existingAmendments = null)
    {
        var signoffs = existingSignoffs ?? new List<SubjectSignoff>();
        var amendments = existingAmendments ?? new List<AmendmentRequest>();

        var unitOfWork = new Mock<IUnitOfWork>();

        unitOfWork.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<ETR.Application.DTOs.Amendment.AmendmentRequestResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<ETR.Application.DTOs.Amendment.AmendmentRequestResponse>> op, CancellationToken ct) => op(ct));
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var subjectResultRepo = new Mock<IGenericRepository<SubjectResult>>();
        subjectResultRepo.Setup(r => r.GetByIdAsync(subjectResult.SubjectResultId, It.IsAny<CancellationToken>())).ReturnsAsync(subjectResult);
        unitOfWork.SetupGet(u => u.SubjectResultRepository).Returns(subjectResultRepo.Object);

        var signoffRepo = new Mock<IGenericRepository<SubjectSignoff>>();
        signoffRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => signoffs);
        unitOfWork.SetupGet(u => u.SubjectSignoffRepository).Returns(signoffRepo.Object);

        var etrRepo = new Mock<IETRCourseRecordRepository>();
        etrRepo.Setup(r => r.GetByIdAsync(subjectResult.EtrId, It.IsAny<CancellationToken>())).ReturnsAsync(etr);
        unitOfWork.SetupGet(u => u.ETRCourseRecordRepository).Returns(etrRepo.Object);

        var amendmentRepo = new Mock<IGenericRepository<AmendmentRequest>>();
        amendmentRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => amendments);
        amendmentRepo.Setup(r => r.AddAsync(It.IsAny<AmendmentRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AmendmentRequest, CancellationToken>((a, _) =>
            {
                a.AmendmentRequestId = amendments.Count + 1000;
                amendments.Add(a);
            })
            .Returns(Task.CompletedTask);
        amendmentRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => amendments.FirstOrDefault(a => a.AmendmentRequestId == id));
        unitOfWork.SetupGet(u => u.AmendmentRequestRepository).Returns(amendmentRepo.Object);

        var auditLogRepo = new Mock<IAuditLogRepository>();
        unitOfWork.SetupGet(u => u.AuditLogRepository).Returns(auditLogRepo.Object);

        var service = new AmendmentService(unitOfWork.Object);
        return (service, unitOfWork, auditLogRepo, amendments, signoffs, subjectResult);
    }

    [Fact]
    public async Task CreateAmendmentRequestAsync_SubjectNotSignedOff_ThrowsBusinessRuleViolation()
    {
        var subjectResult = new SubjectResult { SubjectResultId = 1, EtrId = 10, Status = "Passed" };
        var (service, _, _, _, _, _) = BuildService(etr: new ETRCourseRecord { ETRCourseRecordId = 10, Status = "InProgress", IsLocked = false }, subjectResult);

        await Assert.ThrowsAsync<ETR.Application.Compliance.BusinessRuleViolationException>(
            () => service.CreateAmendmentRequestAsync(1, new CreateAmendmentRequestRequest { Reason = "wrong score" }, requestedByAccountId: 5, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAmendmentRequestAsync_ParentEtrAlreadyCompleted_ThrowsBusinessRuleViolation()
    {
        var subjectResult = new SubjectResult { SubjectResultId = 1, EtrId = 10, Status = "Passed" };
        var signoffs = new List<SubjectSignoff> { new() { SubjectSignoffId = 1, SubjectResultId = 1 } };
        var (service, _, _, _, _, _) = BuildService(etr: new ETRCourseRecord { ETRCourseRecordId = 10, Status = "Completed", IsLocked = true }, subjectResult, signoffs);

        await Assert.ThrowsAsync<ETR.Application.Compliance.BusinessRuleViolationException>(
            () => service.CreateAmendmentRequestAsync(1, new CreateAmendmentRequestRequest { Reason = "wrong score" }, requestedByAccountId: 5, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAmendmentRequestAsync_AlreadyHasPendingRequest_ThrowsBusinessRuleViolation()
    {
        var subjectResult = new SubjectResult { SubjectResultId = 1, EtrId = 10, Status = "Passed" };
        var signoffs = new List<SubjectSignoff> { new() { SubjectSignoffId = 1, SubjectResultId = 1 } };
        var existingAmendments = new List<AmendmentRequest> { new() { AmendmentRequestId = 1, SubjectResultId = 1, Status = "Pending" } };
        var (service, _, _, _, _, _) = BuildService(etr: new ETRCourseRecord { ETRCourseRecordId = 10, Status = "InProgress", IsLocked = false }, subjectResult, signoffs, existingAmendments);

        await Assert.ThrowsAsync<ETR.Application.Compliance.BusinessRuleViolationException>(
            () => service.CreateAmendmentRequestAsync(1, new CreateAmendmentRequestRequest { Reason = "wrong score" }, requestedByAccountId: 5, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAmendmentRequestAsync_ValidRequest_CreatesPendingAmendmentAndWritesAuditLog()
    {
        var subjectResult = new SubjectResult { SubjectResultId = 1, EtrId = 10, Status = "Passed" };
        var signoffs = new List<SubjectSignoff> { new() { SubjectSignoffId = 1, SubjectResultId = 1 } };
        var (service, _, auditLogRepo, amendments, _, _) = BuildService(etr: new ETRCourseRecord { ETRCourseRecordId = 10, Status = "InProgress", IsLocked = false }, subjectResult, signoffs);

        var response = await service.CreateAmendmentRequestAsync(1, new CreateAmendmentRequestRequest { Reason = "wrong score" }, requestedByAccountId: 5, CancellationToken.None);

        Assert.Equal("Pending", response.Status);
        Assert.Equal("Passed", response.OldValue);
        Assert.Single(amendments);
        auditLogRepo.Verify(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveAmendmentRequestAsync_ResetsSubjectResultAndInvalidatesOldSignoffs()
    {
        var subjectResult = new SubjectResult { SubjectResultId = 1, EtrId = 10, Status = "Passed" };
        var signoffs = new List<SubjectSignoff> { new() { SubjectSignoffId = 1, SubjectResultId = 1, IsDeleted = false } };
        var existingAmendments = new List<AmendmentRequest> { new() { AmendmentRequestId = 1, SubjectResultId = 1, Status = "Pending", OldValue = "Passed" } };
        var (service, _, auditLogRepo, _, _, _) = BuildService(etr: new ETRCourseRecord { ETRCourseRecordId = 10, Status = "InProgress", IsLocked = false }, subjectResult, signoffs, existingAmendments);

        var response = await service.ApproveAmendmentRequestAsync(1, new DecideAmendmentRequestRequest { Comment = "ok, fix it" }, approvedByAccountId: 9, CancellationToken.None);

        Assert.Equal("Approved", response.Status);
        Assert.Equal("Pending", subjectResult.Status);
        Assert.True(signoffs[0].IsDeleted);
        auditLogRepo.Verify(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveAmendmentRequestAsync_ParentEtrCompletedSinceRequestWasCreated_Throws()
    {
        var subjectResult = new SubjectResult { SubjectResultId = 1, EtrId = 10, Status = "Passed" };
        var signoffs = new List<SubjectSignoff> { new() { SubjectSignoffId = 1, SubjectResultId = 1 } };
        var existingAmendments = new List<AmendmentRequest> { new() { AmendmentRequestId = 1, SubjectResultId = 1, Status = "Pending", OldValue = "Passed" } };
        var (service, _, _, _, _, _) = BuildService(etr: new ETRCourseRecord { ETRCourseRecordId = 10, Status = "Completed", IsLocked = true }, subjectResult, signoffs, existingAmendments);

        await Assert.ThrowsAsync<ETR.Application.Compliance.BusinessRuleViolationException>(
            () => service.ApproveAmendmentRequestAsync(1, new DecideAmendmentRequestRequest(), approvedByAccountId: 9, CancellationToken.None));
    }

    [Fact]
    public async Task ApproveAmendmentRequestAsync_AlreadyDecided_Throws()
    {
        var subjectResult = new SubjectResult { SubjectResultId = 1, EtrId = 10, Status = "Passed" };
        var existingAmendments = new List<AmendmentRequest> { new() { AmendmentRequestId = 1, SubjectResultId = 1, Status = "Rejected", OldValue = "Passed" } };
        var (service, _, _, _, _, _) = BuildService(etr: new ETRCourseRecord { ETRCourseRecordId = 10, Status = "InProgress", IsLocked = false }, subjectResult, existingAmendments: existingAmendments);

        await Assert.ThrowsAsync<ETR.Application.Compliance.BusinessRuleViolationException>(
            () => service.ApproveAmendmentRequestAsync(1, new DecideAmendmentRequestRequest(), approvedByAccountId: 9, CancellationToken.None));
    }

    [Fact]
    public async Task RejectAmendmentRequestAsync_WithoutComment_ThrowsValidationException()
    {
        var subjectResult = new SubjectResult { SubjectResultId = 1, EtrId = 10, Status = "Passed" };
        var existingAmendments = new List<AmendmentRequest> { new() { AmendmentRequestId = 1, SubjectResultId = 1, Status = "Pending", OldValue = "Passed" } };
        var (service, _, _, _, _, _) = BuildService(etr: new ETRCourseRecord { ETRCourseRecordId = 10, Status = "InProgress", IsLocked = false }, subjectResult, existingAmendments: existingAmendments);

        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
            () => service.RejectAmendmentRequestAsync(1, new DecideAmendmentRequestRequest { Comment = null }, rejectedByAccountId: 9, CancellationToken.None));
    }

    [Fact]
    public async Task RejectAmendmentRequestAsync_WithComment_LeavesSubjectResultUnchanged()
    {
        var subjectResult = new SubjectResult { SubjectResultId = 1, EtrId = 10, Status = "Passed" };
        var existingAmendments = new List<AmendmentRequest> { new() { AmendmentRequestId = 1, SubjectResultId = 1, Status = "Pending", OldValue = "Passed" } };
        var (service, _, auditLogRepo, _, _, _) = BuildService(etr: new ETRCourseRecord { ETRCourseRecordId = 10, Status = "InProgress", IsLocked = false }, subjectResult, existingAmendments: existingAmendments);

        var response = await service.RejectAmendmentRequestAsync(1, new DecideAmendmentRequestRequest { Comment = "score is actually correct" }, rejectedByAccountId: 9, CancellationToken.None);

        Assert.Equal("Rejected", response.Status);
        Assert.Equal("Passed", subjectResult.Status);
        auditLogRepo.Verify(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
