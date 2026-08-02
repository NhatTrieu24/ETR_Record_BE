using ETR.Application.Compliance;
using ETR.Application.DTOs.Evidence.Requests;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using Moq;

namespace ETR.Application.Tests.Services;

public class EvidenceServiceTests
{
    private static (EvidenceService Service, Mock<IUnitOfWork> UnitOfWork) BuildService(EvidenceFile evidence)
    {
        var evidenceRepo = new Mock<IGenericRepository<EvidenceFile>>();
        evidenceRepo.Setup(r => r.GetByIdAsync(evidence.EvidenceFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evidence);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(u => u.EvidenceFileRepository).Returns(evidenceRepo.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return (new EvidenceService(unitOfWork.Object), unitOfWork);
    }

    [Fact]
    public async Task VerifyEvidenceAsync_WhenVerifierUploadedTheEvidence_ExpectsForbiddenAccessException()
    {
        var evidence = new EvidenceFile { EvidenceFileId = 1, UploadedByAccountId = 42, VerificationStatus = "Pending" };
        var (service, _) = BuildService(evidence);
        var request = new VerifyEvidenceRequest { VerificationStatus = "Verified" };

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.VerifyEvidenceAsync(1, request, verifiedByAccountId: 42, CancellationToken.None));
    }

    [Fact]
    public async Task VerifyEvidenceAsync_WhenVerifierDidNotUploadTheEvidence_ExpectsSuccess()
    {
        var evidence = new EvidenceFile { EvidenceFileId = 1, UploadedByAccountId = 42, VerificationStatus = "Pending" };
        var (service, _) = BuildService(evidence);
        var request = new VerifyEvidenceRequest { VerificationStatus = "Verified" };

        var response = await service.VerifyEvidenceAsync(1, request, verifiedByAccountId: 99, CancellationToken.None);

        Assert.Equal("Verified", response.VerificationStatus);
        Assert.Equal(99, response.VerifiedByAccountId);
    }
}
