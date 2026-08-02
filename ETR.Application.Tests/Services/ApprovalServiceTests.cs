using ETR.Application.Compliance;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using Moq;

namespace ETR.Application.Tests.Services;

public class ApprovalServiceTests
{
    private static ApprovalService BuildService()
    {
        // Role/action validation runs before any repository access, so an unconfigured mock is enough
        // for the negative-path tests below — they must never touch _unitOfWork at all.
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var etrService = new Mock<IEtrService>(MockBehavior.Strict);
        return new ApprovalService(unitOfWork.Object, etrService.Object);
    }

    [Theory]
    [InlineData("Verify", "Instructor")]
    [InlineData("Verify", "TrainingManager")]
    [InlineData("Approve", "Instructor")]
    [InlineData("Approve", "QA")]
    [InlineData("Reject", "Instructor")]
    [InlineData("Return", "Instructor")]
    public async Task ProcessApprovalActionAsync_WhenRoleNotAllowedForAction_ExpectsForbiddenAccessException(string action, string roleName)
    {
        var service = BuildService();

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.ProcessApprovalActionAsync(1, action, actionByAccountId: 1, actionByRoleName: roleName, comment: "reason", CancellationToken.None));
    }

    [Theory]
    [InlineData("Verify", "QA")]
    [InlineData("Verify", "Admin")]
    [InlineData("Approve", "TrainingManager")]
    [InlineData("Approve", "Admin")]
    [InlineData("Reject", "QA")]
    [InlineData("Return", "QA")]
    public async Task ProcessApprovalActionAsync_WhenRoleIsAllowedForAction_ExpectsNoRoleRejection(string action, string roleName)
    {
        var service = BuildService();

        // These roles ARE authorized — the call proceeds past the role check into repository access,
        // which the strict unmocked IUnitOfWork will reject with a MockException, not
        // ForbiddenAccessException. That distinction is exactly what this test verifies.
        var ex = await Record.ExceptionAsync(
            () => service.ProcessApprovalActionAsync(1, action, actionByAccountId: 1, actionByRoleName: roleName, comment: "reason", CancellationToken.None));

        Assert.False(ex is ForbiddenAccessException);
    }

    [Fact]
    public async Task ProcessApprovalActionAsync_WhenActionIsUnknown_ExpectsBusinessRuleViolationException()
    {
        var service = BuildService();

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => service.ProcessApprovalActionAsync(1, "Delete", actionByAccountId: 1, actionByRoleName: "Admin", comment: null, CancellationToken.None));
    }
}
