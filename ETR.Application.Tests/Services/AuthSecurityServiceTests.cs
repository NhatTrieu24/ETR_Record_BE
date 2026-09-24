using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Application.Services;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ETR.Application.Tests.Services;

public class AuthSecurityServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IGenericRepository<Account>> _accountRepoMock = new();
    private readonly Mock<IGenericRepository<UserProfile>> _profileRepoMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<ILogger<AuthSecurityService>> _loggerMock = new();

    public AuthSecurityServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.AccountRepository).Returns(_accountRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.UserProfileRepository).Returns(_profileRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private AuthSecurityService CreateService()
    {
        return new AuthSecurityService(
            _unitOfWorkMock.Object,
            _emailServiceMock.Object,
            _cache,
            _configMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ForgotPasswordAsync_SendsResetEmail_WhenAccountExists()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("OldPass123");
        var account = new Account
        {
            AccountId = 1,
            Username = "user@example.com",
            PasswordHash = passwordHash,
            Status = AccountStatus.Active
        };
        var profile = new UserProfile
        {
            AccountId = 1,
            FullName = "John Doe",
            Email = "user@example.com"
        };

        _accountRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });
        _profileRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { profile });

        var service = CreateService();

        await service.ForgotPasswordAsync("user@example.com", "http://localhost:5173", CancellationToken.None);

        _emailServiceMock.Verify(e => e.SendTemplatedEmailAsync(
            "user@example.com",
            "John Doe",
            "PasswordReset.html",
            It.IsAny<string>(),
            It.Is<IReadOnlyDictionary<string, string>>(d =>
                d["FullName"] == "John Doe" &&
                !string.IsNullOrEmpty(d["OtpCode"]) &&
                d["ExpiresInMinutes"] == "15"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_Succeeds_WithValidOtpCode()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("OldPass123");
        var account = new Account
        {
            AccountId = 1,
            Username = "user@example.com",
            PasswordHash = passwordHash,
            Status = AccountStatus.Active
        };

        _accountRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });
        _profileRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile>());
        _accountRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        string capturedOtp = "";
        _emailServiceMock.Setup(e => e.SendTemplatedEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string, IReadOnlyDictionary<string, string>, CancellationToken>(
                (_, _, _, _, tokens, _) => capturedOtp = tokens["OtpCode"])
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // 1. Request forgot password
        await service.ForgotPasswordAsync("user@example.com", null, CancellationToken.None);
        Assert.NotEmpty(capturedOtp);

        // 2. Reset with captured OTP
        await service.ResetPasswordAsync(capturedOtp, "NewSecret456", "user@example.com", CancellationToken.None);

        // 3. Verify new password was hashed and saved
        Assert.True(BCrypt.Net.BCrypt.Verify("NewSecret456", account.PasswordHash));
        _unitOfWorkMock.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_Throws_WhenTokenIsInvalid()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            service.ResetPasswordAsync("invalid-token-123", "NewSecret456", "user@example.com", CancellationToken.None));
    }

    [Fact]
    public async Task ChangePasswordAsync_Succeeds_WhenOldPasswordIsCorrect()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectOldPass");
        var account = new Account
        {
            AccountId = 5,
            Username = "test@example.com",
            PasswordHash = passwordHash,
            Status = AccountStatus.Active
        };

        _accountRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var service = CreateService();

        await service.ChangePasswordAsync(5, "CorrectOldPass", "BrandNewPass99", CancellationToken.None);

        Assert.True(BCrypt.Net.BCrypt.Verify("BrandNewPass99", account.PasswordHash));
        _unitOfWorkMock.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_Throws_WhenOldPasswordIsIncorrect()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectOldPass");
        var account = new Account
        {
            AccountId = 5,
            Username = "test@example.com",
            PasswordHash = passwordHash,
            Status = AccountStatus.Active
        };

        _accountRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var service = CreateService();

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            service.ChangePasswordAsync(5, "WrongOldPass", "BrandNewPass99", CancellationToken.None));
    }

    [Fact]
    public async Task SendEmailVerification_And_VerifyEmail_Succeeds()
    {
        string capturedOtp = "";
        _emailServiceMock.Setup(e => e.SendTemplatedEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string, IReadOnlyDictionary<string, string>, CancellationToken>(
                (_, _, _, _, tokens, _) => capturedOtp = tokens["OtpCode"])
            .Returns(Task.CompletedTask);

        _accountRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());
        _profileRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile>());

        var service = CreateService();

        // Check not verified initially
        Assert.False(service.IsEmailVerified("verify@example.com"));

        // Send verification
        await service.SendEmailVerificationAsync("verify@example.com", null, CancellationToken.None);
        Assert.NotEmpty(capturedOtp);

        // Verify with OTP
        await service.VerifyEmailAsync(capturedOtp, "verify@example.com", CancellationToken.None);

        // Check verified
        Assert.True(service.IsEmailVerified("verify@example.com"));
    }
}
