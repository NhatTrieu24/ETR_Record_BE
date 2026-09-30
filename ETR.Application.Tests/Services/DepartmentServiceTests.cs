using ETR.Application.Compliance;
using ETR.Application.DTOs.Department;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Moq;

namespace ETR.Application.Tests.Services;

public class DepartmentServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IGenericRepository<Department>> _mockDeptRepo;
    private readonly Mock<IGenericRepository<Account>> _mockAccountRepo;
    private readonly Mock<IAuditLogRepository> _mockAuditRepo;
    private readonly DepartmentService _service;

    public DepartmentServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockDeptRepo = new Mock<IGenericRepository<Department>>();
        _mockAccountRepo = new Mock<IGenericRepository<Account>>();
        _mockAuditRepo = new Mock<IAuditLogRepository>();

        _mockUow.Setup(u => u.DepartmentRepository).Returns(_mockDeptRepo.Object);
        _mockUow.Setup(u => u.AccountRepository).Returns(_mockAccountRepo.Object);
        _mockUow.Setup(u => u.AuditLogRepository).Returns(_mockAuditRepo.Object);

        _service = new DepartmentService(_mockUow.Object);
    }

    [Fact]
    public async Task DeleteDepartmentAsync_ShouldThrowKeyNotFound_WhenDepartmentDoesNotExist()
    {
        _mockDeptRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.DeleteDepartmentAsync(999, deletedByAccountId: 1));
    }

    [Theory]
    [InlineData(1, "Administration")]
    [InlineData(2, "Training")]
    [InlineData(3, "Administration")]
    [InlineData(4, "training")]
    public async Task DeleteDepartmentAsync_ShouldThrowBusinessRule_WhenDeletingCoreDepartment(int id, string name)
    {
        var dept = new Department { DepartmentId = id, DepartmentName = name };
        _mockDeptRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dept);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.DeleteDepartmentAsync(id, deletedByAccountId: 1));

        Assert.Contains("Không thể xóa các phòng ban mặc định cốt lõi", ex.Message);
    }

    [Fact]
    public async Task DeleteDepartmentAsync_ShouldThrowBusinessRule_WhenDepartmentHasActiveOrInactiveAccounts()
    {
        int deptId = 5;
        var dept = new Department { DepartmentId = deptId, DepartmentName = "IT Support" };
        _mockDeptRepo.Setup(r => r.GetByIdAsync(deptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dept);

        // Accounts include both Active and Inactive
        var accounts = new List<Account>
        {
            new() { AccountId = 10, DepartmentId = deptId, Status = AccountStatus.Inactive, IsDeleted = false },
            new() { AccountId = 11, DepartmentId = deptId, Status = AccountStatus.Active, IsDeleted = false },
            new() { AccountId = 12, DepartmentId = deptId, Status = AccountStatus.Active, IsDeleted = true }, // soft deleted -> should not block
            new() { AccountId = 13, DepartmentId = 6, Status = AccountStatus.Active, IsDeleted = false } // different dept
        };

        _mockAccountRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.DeleteDepartmentAsync(deptId, deletedByAccountId: 1));

        Assert.Contains("Không thể xóa phòng ban 'IT Support' vì hiện đang có 2 nhân sự/tài khoản trực thuộc", ex.Message);
    }

    [Fact]
    public async Task DeleteDepartmentAsync_ShouldSucceed_WhenNoActiveAccountsInDepartment()
    {
        int deptId = 5;
        var dept = new Department { DepartmentId = deptId, DepartmentName = "Quality Assurance" };
        _mockDeptRepo.Setup(r => r.GetByIdAsync(deptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dept);

        // Only soft-deleted accounts or accounts in other departments
        var accounts = new List<Account>
        {
            new() { AccountId = 10, DepartmentId = deptId, Status = AccountStatus.Active, IsDeleted = true },
            new() { AccountId = 11, DepartmentId = 6, Status = AccountStatus.Active, IsDeleted = false }
        };

        _mockAccountRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        await _service.DeleteDepartmentAsync(deptId, deletedByAccountId: 1);

        Assert.True(dept.IsDeleted);
        Assert.NotNull(dept.DeletedAt);
        Assert.Equal(1, dept.UpdatedByAccountId);

        _mockDeptRepo.Verify(r => r.Update(dept), Times.Once);
        _mockAuditRepo.Verify(r => r.AddAsync(It.Is<AuditLog>(a =>
            a.EntityName == nameof(Department) &&
            a.RecordId == deptId &&
            a.ActionType == AuditActionType.DELETE.ToString()), It.IsAny<CancellationToken>()), Times.Once);
        _mockUow.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
