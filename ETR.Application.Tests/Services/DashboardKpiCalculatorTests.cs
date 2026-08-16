using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using Moq;

namespace ETR.Application.Tests.Services;

public class DashboardKpiCalculatorTests
{
    private static Mock<IGenericRepository<T>> MockRepo<T>(IReadOnlyList<T> items) where T : BaseEntity
    {
        var repo = new Mock<IGenericRepository<T>>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(items);
        return repo;
    }

    [Fact]
    public async Task ComputeSystemStatsAsync_counts_accounts_by_role_and_activity()
    {
        var roles = new List<Role>
        {
            new() { RoleId = 1, RoleName = "Student" },
            new() { RoleId = 2, RoleName = "Instructor" },
            new() { RoleId = 3, RoleName = "Admin" }
        };
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var accounts = new List<Account>
        {
            new() { AccountId = 1, RoleId = 1, IsActive = true, CreatedAt = monthStart.AddDays(1) },
            new() { AccountId = 2, RoleId = 1, IsActive = false, CreatedAt = monthStart.AddMonths(-2) },
            new() { AccountId = 3, RoleId = 2, IsActive = true, CreatedAt = monthStart.AddMonths(-1) },
            new() { AccountId = 4, RoleId = 3, IsActive = true, CreatedAt = monthStart.AddDays(2) }
        };
        var courses = new List<Course> { new() { CourseId = 1 }, new() { CourseId = 2 } };
        var classes = new List<Class> { new() { ClassId = 1 } };

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.AccountRepository).Returns(MockRepo(accounts).Object);
        unitOfWork.Setup(u => u.RoleRepository).Returns(MockRepo(roles).Object);
        unitOfWork.Setup(u => u.CourseRepository).Returns(MockRepo(courses).Object);
        unitOfWork.Setup(u => u.ClassRepository).Returns(MockRepo(classes).Object);

        var result = await DashboardKpiCalculator.ComputeSystemStatsAsync(unitOfWork.Object, CancellationToken.None);

        Assert.Equal(4, result.TotalUsers);
        Assert.Equal(2, result.TotalLearners);
        Assert.Equal(1, result.TotalInstructors);
        Assert.Equal(2, result.TotalCourses);
        Assert.Equal(1, result.TotalClasses);
        Assert.Equal(3, result.ActiveAccounts);
        Assert.Equal(2, result.NewUsersThisMonth);
    }

    [Fact]
    public async Task ComputeMonthlyTrendAsync_buckets_locked_and_returned_by_month()
    {
        var now = DateTime.UtcNow;
        var thisMonth = new DateTime(now.Year, now.Month, 1);
        var lastMonth = thisMonth.AddMonths(-1);

        var etrs = new List<ETRCourseRecord>
        {
            new() { ETRCourseRecordId = 1, IsLocked = true, CompletedAt = thisMonth.AddDays(2) },
            new() { ETRCourseRecordId = 2, IsLocked = true, CompletedAt = lastMonth.AddDays(2) },
            new() { ETRCourseRecordId = 3, IsLocked = false, CompletedAt = thisMonth.AddDays(3) }
        };
        var approvalHistory = new List<ApprovalHistory>
        {
            new() { ApprovalHistoryId = 1, NewStatus = "ReturnedForCorrection", ActionAt = thisMonth.AddDays(1) },
            new() { ApprovalHistoryId = 2, NewStatus = "Approved", ActionAt = thisMonth.AddDays(1) }
        };

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.ETRCourseRecordRepository.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(etrs);
        unitOfWork.Setup(u => u.ApprovalHistoryRepository).Returns(MockRepo(approvalHistory).Object);

        var result = await DashboardKpiCalculator.ComputeMonthlyTrendAsync(unitOfWork.Object, 8, CancellationToken.None);

        Assert.Equal(8, result.Months.Count);
        Assert.Equal(thisMonth.ToString("yyyy-MM"), result.Months[^1]);
        Assert.Equal(1, result.Locked[^1]);
        Assert.Equal(1, result.Locked[^2]);
        Assert.Equal(1, result.Returned[^1]);
    }

    [Fact]
    public async Task ComputeLockedRecordsSummaryAsync_computes_compliance_rate()
    {
        var etrs = new List<ETRCourseRecord>
        {
            new() { ETRCourseRecordId = 1, IsLocked = true },
            new() { ETRCourseRecordId = 2, IsLocked = true },
            new() { ETRCourseRecordId = 3, IsLocked = false },
            new() { ETRCourseRecordId = 4, IsLocked = false }
        };

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.ETRCourseRecordRepository.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(etrs);

        var result = await DashboardKpiCalculator.ComputeLockedRecordsSummaryAsync(unitOfWork.Object, CancellationToken.None);

        Assert.Equal(2, result.TotalLocked);
        Assert.Equal(50m, result.ComplianceRate);
    }

    [Fact]
    public async Task ComputeEvidenceSummaryAsync_counts_by_verification_status()
    {
        var evidenceFiles = new List<EvidenceFile>
        {
            new() { EvidenceFileId = 1, VerificationStatus = "Verified" },
            new() { EvidenceFileId = 2, VerificationStatus = "Verified" },
            new() { EvidenceFileId = 3, VerificationStatus = "Pending" },
            new() { EvidenceFileId = 4, VerificationStatus = "Rejected" }
        };

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.EvidenceFileRepository).Returns(MockRepo(evidenceFiles).Object);

        var result = await DashboardKpiCalculator.ComputeEvidenceSummaryAsync(unitOfWork.Object, CancellationToken.None);

        Assert.Equal(4, result.Total);
        Assert.Equal(2, result.Verified);
        Assert.Equal(1, result.Pending);
        Assert.Equal(1, result.Rejected);
    }
}
