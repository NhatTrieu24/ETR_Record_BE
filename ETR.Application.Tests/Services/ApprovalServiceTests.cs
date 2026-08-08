using ETR.Application.Compliance;
using ETR.Application.DTOs.Approval;
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
    [InlineData(ApprovalActionType.Verify, "Instructor")]
    [InlineData(ApprovalActionType.Verify, "TrainingManager")]
    [InlineData(ApprovalActionType.Approve, "Instructor")]
    [InlineData(ApprovalActionType.Approve, "QA")]
    [InlineData(ApprovalActionType.Reject, "Instructor")]
    [InlineData(ApprovalActionType.Return, "Instructor")]
    public async Task ProcessApprovalActionAsync_WhenRoleNotAllowedForAction_ExpectsForbiddenAccessException(ApprovalActionType action, string roleName)
    {
        var service = BuildService();

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.ProcessApprovalActionAsync(1, action, actionByAccountId: 1, actionByRoleName: roleName, comment: "reason", CancellationToken.None));
    }

    [Theory]
    [InlineData(ApprovalActionType.Verify, "QA")]
    [InlineData(ApprovalActionType.Verify, "Admin")]
    [InlineData(ApprovalActionType.Approve, "TrainingManager")]
    [InlineData(ApprovalActionType.Approve, "Admin")]
    [InlineData(ApprovalActionType.Reject, "QA")]
    [InlineData(ApprovalActionType.Return, "QA")]
    public async Task ProcessApprovalActionAsync_WhenRoleIsAllowedForAction_ExpectsNoRoleRejection(ApprovalActionType action, string roleName)
    {
        var service = BuildService();

        // These roles ARE authorized — the call proceeds past the role check into repository access,
        // which the strict unmocked IUnitOfWork will reject with a MockException, not
        // ForbiddenAccessException. That distinction is exactly what this test verifies.
        var ex = await Record.ExceptionAsync(
            () => service.ProcessApprovalActionAsync(1, action, actionByAccountId: 1, actionByRoleName: roleName, comment: "reason", CancellationToken.None));

        Assert.False(ex is ForbiddenAccessException);
    }

    // "Unknown action" is no longer reachable here — action is a real ApprovalActionType enum now
    // (mục #6, docs/todo/9.todo_to_complete_system.md), so ASP.NET Core model binding rejects any
    // value outside {Verify, Approve, Reject, Return} with a 400 before ApprovalService is even
    // called. There is nothing left for ApprovalService itself to validate on that front.
}
