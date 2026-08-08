using ETR.Application.DTOs.CompletionRequirement;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using Moq;

namespace ETR.Application.Tests.Services;

public class CompletionRequirementServiceTests
{
    private static (CompletionRequirementService Service, Mock<IUnitOfWork> UnitOfWork, Course Course, List<CompletionRequirement> Requirements, Mock<IAuditLogRepository> AuditLogRepo)
        BuildService(Course course, List<CompletionRequirement> requirements)
    {
        var unitOfWork = new Mock<IUnitOfWork>();

        unitOfWork.Setup(u => u.ExecuteInStrategyAsync(It.IsAny<Func<CancellationToken, Task<CompletionRequirementResponse>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<CompletionRequirementResponse>> op, CancellationToken ct) => op(ct));
        unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var courseRepo = new Mock<IGenericRepository<Course>>();
        courseRepo.Setup(r => r.GetByIdAsync(course.CourseId, It.IsAny<CancellationToken>())).ReturnsAsync(course);
        unitOfWork.SetupGet(u => u.CourseRepository).Returns(courseRepo.Object);

        var requirementRepo = new Mock<IGenericRepository<CompletionRequirement>>();
        requirementRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => requirements);
        requirementRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => requirements.FirstOrDefault(x => x.RequirementId == id));
        requirementRepo.Setup(r => r.AddAsync(It.IsAny<CompletionRequirement>(), It.IsAny<CancellationToken>()))
            .Callback<CompletionRequirement, CancellationToken>((r, _) =>
            {
                r.RequirementId = requirements.Count + 1000;
                requirements.Add(r);
            })
            .Returns(Task.CompletedTask);
        unitOfWork.SetupGet(u => u.CompletionRequirementRepository).Returns(requirementRepo.Object);

        var auditLogRepo = new Mock<IAuditLogRepository>();
        unitOfWork.SetupGet(u => u.AuditLogRepository).Returns(auditLogRepo.Object);

        return (new CompletionRequirementService(unitOfWork.Object), unitOfWork, course, requirements, auditLogRepo);
    }

    [Fact]
    public async Task UpdateCompletionRequirementAsync_ThresholdChanged_ClosesOldRowAndCreatesNewVersionedRow()
    {
        var course = new Course { CourseId = 1, CourseCode = "C1", VersionNo = 1 };
        var oldRequirement = new CompletionRequirement
        {
            RequirementId = 1, CourseId = 1, RequirementName = "Min Attendance", RequirementType = "MinAttendance",
            ThresholdValue = 80m, IsMandatory = true, VersionNo = 1, EffectiveFrom = DateTime.UtcNow.AddMonths(-2), EffectiveTo = null
        };
        var (service, _, _, requirements, auditLogRepo) = BuildService(course, new List<CompletionRequirement> { oldRequirement });

        var request = new UpdateCompletionRequirementRequest
        {
            RequirementName = "Min Attendance", IsMandatory = true, DisplayOrder = 1,
            RequirementType = "MinAttendance", ThresholdValue = 90m
        };

        var response = await service.UpdateCompletionRequirementAsync(1, request, updatedByAccountId: 9, CancellationToken.None);

        // Old row closed, not deleted — still queryable for history/audit.
        Assert.NotNull(oldRequirement.EffectiveTo);
        Assert.Equal(80m, oldRequirement.ThresholdValue);
        Assert.Equal(1, oldRequirement.VersionNo);

        // New row created with the updated threshold and the bumped Course VersionNo.
        Assert.NotEqual(1, response.RequirementId);
        Assert.Equal(90m, response.ThresholdValue);
        Assert.Equal(2, response.VersionNo);
        Assert.Equal(2, course.VersionNo);
        Assert.Equal(2, requirements.Count);

        auditLogRepo.Verify(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCompletionRequirementAsync_OnlyCosmeticFieldsChanged_UpdatesInPlaceWithoutVersioning()
    {
        var course = new Course { CourseId = 1, CourseCode = "C1", VersionNo = 1 };
        var requirement = new CompletionRequirement
        {
            RequirementId = 1, CourseId = 1, RequirementName = "Min Attendance", RequirementType = "MinAttendance",
            ThresholdValue = 80m, IsMandatory = true, VersionNo = 1, EffectiveFrom = DateTime.UtcNow.AddMonths(-2), EffectiveTo = null
        };
        var (service, _, _, requirements, _) = BuildService(course, new List<CompletionRequirement> { requirement });

        var request = new UpdateCompletionRequirementRequest
        {
            RequirementName = "Minimum Attendance Rate", IsMandatory = true, DisplayOrder = 2,
            RequirementType = "MinAttendance", ThresholdValue = 80m
        };

        var response = await service.UpdateCompletionRequirementAsync(1, request, updatedByAccountId: 9, CancellationToken.None);

        Assert.Equal(1, response.RequirementId);
        Assert.Equal("Minimum Attendance Rate", response.RequirementName);
        Assert.Equal(1, response.VersionNo);
        Assert.Equal(1, course.VersionNo);
        Assert.Single(requirements);
        Assert.Null(requirement.EffectiveTo);
    }

    [Fact]
    public async Task GetCompletionRequirementsByCourseAsync_OnlyReturnsCurrentlyEffectiveRows()
    {
        var course = new Course { CourseId = 1, CourseCode = "C1", VersionNo = 2 };
        var oldRow = new CompletionRequirement { RequirementId = 1, CourseId = 1, VersionNo = 1, EffectiveTo = DateTime.UtcNow.AddDays(-1) };
        var currentRow = new CompletionRequirement { RequirementId = 2, CourseId = 1, VersionNo = 2, EffectiveTo = null };
        var (service, _, _, _, _) = BuildService(course, new List<CompletionRequirement> { oldRow, currentRow });

        var result = (await service.GetCompletionRequirementsByCourseAsync(1, CancellationToken.None)).ToList();

        Assert.Single(result);
        Assert.Equal(2, result[0].RequirementId);
    }
}
