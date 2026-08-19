using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace ETR.Application.Tests.Services;

public class AccountServiceTests
{
    private static Mock<IUnitOfWork> BuildUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var accountRepo = new Mock<IGenericRepository<Account>>();
        accountRepo.Setup(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Callback<Account, CancellationToken>((a, _) => a.AccountId = 99)
            .Returns(Task.CompletedTask);
        unitOfWork.Setup(u => u.AccountRepository).Returns(accountRepo.Object);
        unitOfWork.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return unitOfWork;
    }

    private static CreateAccountRequest BuildRequest() =>
        new("newuser@example.com", "P@ssw0rd123", 1, 1);

    [Fact]
    public async Task CreateAccountAsync_SendsAccountCreatedEmail_ToTheNewUsername()
    {
        var unitOfWork = BuildUnitOfWork();
        var emailService = new Mock<IEmailService>();
        var logger = Mock.Of<ILogger<AccountService>>();
        var currentUserService = Mock.Of<ICurrentUserService>();

        var service = new AccountService(unitOfWork.Object, currentUserService, emailService.Object, logger);
        var request = BuildRequest();

        await service.CreateAccountAsync(request, createdByAccountId: 1, isCallerAdmin: true, CancellationToken.None);

        emailService.Verify(e => e.SendTemplatedEmailAsync(
            "newuser@example.com",
            "newuser@example.com",
            "AccountCreated.html",
            It.IsAny<string>(),
            It.Is<IReadOnlyDictionary<string, string>>(t =>
                t["Username"] == "newuser@example.com" && t["TemporaryPassword"] == "P@ssw0rd123"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAccountAsync_StillReturnsAccount_WhenEmailSendingFails()
    {
        var unitOfWork = BuildUnitOfWork();
        var emailService = new Mock<IEmailService>();
        emailService.Setup(e => e.SendTemplatedEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));
        var logger = Mock.Of<ILogger<AccountService>>();
        var currentUserService = Mock.Of<ICurrentUserService>();

        var service = new AccountService(unitOfWork.Object, currentUserService, emailService.Object, logger);
        var request = BuildRequest();

        var result = await service.CreateAccountAsync(request, createdByAccountId: 1, isCallerAdmin: true, CancellationToken.None);

        Assert.Equal(99, result.AccountId);
    }
}
