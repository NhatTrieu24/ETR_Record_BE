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
    private readonly Mock<IAuditLogRepository> _auditLogRepoMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly Mock<ILogger<AuthSecurityService>> _loggerMock = new();

    public AuthSecurityServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.AccountRepository).Returns(_accountRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.UserProfileRepository).Returns(_profileRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.AuditLogRepository).Returns(_auditLogRepoMock.Object);
        _auditLogRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private AuthSecurityService CreateService()
    {
        return new AuthSecurityService(
            _unitOfWorkMock.Object,
            _tokenServiceMock.Object,
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

    [Fact]
    public async Task AuthenticateAsync_ReturnsAuthResponse_WhenCredentialsAreValid()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("SecretPassword123");
        var account = new Account
        {
            AccountId = 10,
            Username = "pilot1@aviation.com",
            PasswordHash = passwordHash,
            RoleId = 2,
            Status = AccountStatus.Active
        };
        var role = new Role { RoleId = 2, RoleName = "Instructor" };
        var profile = new UserProfile { AccountId = 10, FullName = "Captain Miller" };

        var roleRepoMock = new Mock<IGenericRepository<Role>>();
        _unitOfWorkMock.Setup(u => u.RoleRepository).Returns(roleRepoMock.Object);
        roleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role> { role });

        _accountRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });
        _profileRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { profile });
        _tokenServiceMock.Setup(t => t.GenerateToken(account, role))
            .Returns("valid-jwt-token");

        var service = CreateService();

        var result = await service.AuthenticateAsync(new LoginRequestDto("pilot1@aviation.com", "SecretPassword123"), CancellationToken.None);

        Assert.Equal(10, result.AccountId);
        Assert.Equal("pilot1@aviation.com", result.Username);
        Assert.Equal("Captain Miller", result.FullName);
        Assert.Equal("Instructor", result.Role);
        Assert.Equal("valid-jwt-token", result.Token);
    }

    [Fact]
    public async Task AuthenticateAsync_ThrowsUnauthorized_WhenPasswordIsIncorrect()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("SecretPassword123");
        var account = new Account
        {
            AccountId = 10,
            Username = "pilot1@aviation.com",
            PasswordHash = passwordHash,
            RoleId = 2,
            Status = AccountStatus.Active
        };

        _accountRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.AuthenticateAsync(new LoginRequestDto("pilot1@aviation.com", "WrongPassword!"), CancellationToken.None));
    }
}
