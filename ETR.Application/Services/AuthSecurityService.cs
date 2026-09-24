using System.Security.Cryptography;
using ETR.Application.Compliance;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ETR.Application.Services;

public class AuthSecurityService : IAuthSecurityService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthSecurityService> _logger;

    public AuthSecurityService(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<AuthSecurityService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ForgotPasswordAsync(string email, string? clientBaseUrl = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new BusinessRuleViolationException("Vui lòng nhập địa chỉ email hợp lệ.");
        }

        var cleanEmail = email.Trim().ToLowerInvariant();

        // Tìm account theo Username hoặc UserProfile.Email
        var accounts = await _unitOfWork.AccountRepository.GetAllAsync(cancellationToken);
        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);

        var account = accounts.FirstOrDefault(a =>
            a.Username.ToLower() == cleanEmail ||
            profiles.Any(p => p.AccountId == a.AccountId && p.Email.ToLower() == cleanEmail));

        // Bảo vệ chống Account Enumeration: Dù email không tồn tại hoặc inactive, vẫn không báo lỗi lộ thông tin
        if (account == null || account.Status != AccountStatus.Active)
        {
            _logger.LogInformation("Yêu cầu quên mật khẩu cho email không tồn tại hoặc inactive: {Email}", cleanEmail);
            return;
        }

        var profile = profiles.FirstOrDefault(p => p.AccountId == account.AccountId);
        var fullName = profile?.FullName ?? account.Username;

        // Sinh mã OTP 6 chữ số ngẫu nhiên & Token bảo mật URL-safe 64 ký tự
        var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToHexString(tokenBytes).ToLowerInvariant();

        var resetEntry = new PasswordResetEntry
        {
            AccountId = account.AccountId,
            Email = cleanEmail,
            Token = token,
            OtpCode = otpCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CurrentPasswordHash = account.PasswordHash
        };

        // Cache lưu trữ với thời hạn 15 phút
        var cacheExpiry = TimeSpan.FromMinutes(15);
        _cache.Set($"pwd_reset_token_{token}", resetEntry, cacheExpiry);
        _cache.Set($"pwd_reset_otp_{cleanEmail}_{otpCode}", resetEntry, cacheExpiry);
        _cache.Set($"pwd_reset_otp_direct_{otpCode}", resetEntry, cacheExpiry);

        // Tạo liên kết đặt lại mật khẩu
        var baseUrl = clientBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";
        }
        baseUrl = baseUrl.TrimEnd('/');
        var resetLink = $"{baseUrl}/reset-password?token={token}&email={Uri.EscapeDataString(cleanEmail)}";

        try
        {
            await _emailService.SendTemplatedEmailAsync(
                toEmail: cleanEmail,
                toName: fullName,
                templateName: "PasswordReset.html",
                subject: "Yêu cầu đặt lại mật khẩu - ETR Management",
                tokens: new Dictionary<string, string>
                {
                    ["FullName"] = fullName,
                    ["OtpCode"] = otpCode,
                    ["ResetLink"] = resetLink,
                    ["ExpiresInMinutes"] = "15"
                },
                cancellationToken: cancellationToken);

            _logger.LogInformation("Đã gửi email đặt lại mật khẩu đến {Email} thành công.", cleanEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi email đặt lại mật khẩu cho {Email}.", cleanEmail);
            throw new BusinessRuleViolationException("Không thể gửi email lúc này. Vui lòng kiểm tra lại dịch vụ email hệ thống hoặc thử lại sau.");
        }
    }

    public async Task ResetPasswordAsync(string token, string newPassword, string? email = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new BusinessRuleViolationException("Mã xác thực hoặc token không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            throw new BusinessRuleViolationException("Mật khẩu mới phải có ít nhất 6 ký tự để đảm bảo an toàn.");
        }

        var cleanToken = token.Trim();
        PasswordResetEntry? entry = null;

        // 1. Thử tìm qua token chuỗi dài
        if (_cache.TryGetValue($"pwd_reset_token_{cleanToken.ToLowerInvariant()}", out PasswordResetEntry? tokenEntry))
        {
            entry = tokenEntry;
        }
        // 2. Thử tìm qua OTP kèm email
        else if (!string.IsNullOrWhiteSpace(email) &&
                 _cache.TryGetValue($"pwd_reset_otp_{email.Trim().ToLowerInvariant()}_{cleanToken}", out PasswordResetEntry? otpEmailEntry))
        {
            entry = otpEmailEntry;
        }
        // 3. Thử tìm qua OTP trực tiếp
        else if (_cache.TryGetValue($"pwd_reset_otp_direct_{cleanToken}", out PasswordResetEntry? otpDirectEntry))
        {
            entry = otpDirectEntry;
        }

        if (entry == null || entry.ExpiresAt < DateTime.UtcNow)
        {
            throw new BusinessRuleViolationException("Mã xác thực hoặc liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn (chỉ có hiệu lực trong 15 phút). Vui lòng gửi lại yêu cầu mới.");
        }

        var account = await _unitOfWork.AccountRepository.GetByIdAsync(entry.AccountId, cancellationToken);
        if (account == null || account.Status != AccountStatus.Active)
        {
            throw new BusinessRuleViolationException("Tài khoản không tồn tại hoặc đã bị vô hiệu hóa.");
        }

        // Chống replay attack: nếu mật khẩu đã được đổi kể từ lúc tạo yêu cầu reset
        if (account.PasswordHash != entry.CurrentPasswordHash)
        {
            throw new BusinessRuleViolationException("Mật khẩu đã được thay đổi trước đó. Yêu cầu đặt lại mật khẩu này không còn hiệu lực.");
        }

        // Cập nhật mật khẩu mới được hash bằng BCrypt
        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        account.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveAsync(cancellationToken);

        // Hủy bỏ các entry trong cache để không thể dùng lại
        _cache.Remove($"pwd_reset_token_{entry.Token}");
        _cache.Remove($"pwd_reset_otp_{entry.Email}_{entry.OtpCode}");
        _cache.Remove($"pwd_reset_otp_direct_{entry.OtpCode}");

        _logger.LogInformation("Đặt lại mật khẩu thành công cho AccountId {AccountId}, Email {Email}.", account.AccountId, entry.Email);

        // Gửi email thông báo bảo mật xác nhận mật khẩu đã thay đổi
        try
        {
            var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
            var profile = profiles.FirstOrDefault(p => p.AccountId == account.AccountId);
            var fullName = profile?.FullName ?? account.Username;

            await _emailService.SendTemplatedEmailAsync(
                toEmail: entry.Email,
                toName: fullName,
                templateName: "PasswordChangedConfirmation.html",
                subject: "Mật khẩu tài khoản ETR Management đã được thay đổi",
                tokens: new Dictionary<string, string>
                {
                    ["FullName"] = fullName,
                    ["ChangedAt"] = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss")
                },
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gửi email thông báo đổi mật khẩu thất bại cho AccountId {AccountId}.", account.AccountId);
        }
    }

    public async Task ChangePasswordAsync(int accountId, string oldPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(oldPassword))
        {
            throw new BusinessRuleViolationException("Vui lòng nhập mật khẩu hiện tại.");
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            throw new BusinessRuleViolationException("Mật khẩu mới phải có ít nhất 6 ký tự để đảm bảo an toàn.");
        }

        if (oldPassword == newPassword)
        {
            throw new BusinessRuleViolationException("Mật khẩu mới không được trùng với mật khẩu hiện tại.");
        }

        var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account == null || account.Status != AccountStatus.Active)
        {
            throw new BusinessRuleViolationException("Tài khoản không tồn tại hoặc đã bị vô hiệu hóa.");
        }

        if (!BCrypt.Net.BCrypt.Verify(oldPassword, account.PasswordHash))
        {
            throw new BusinessRuleViolationException("Mật khẩu hiện tại không chính xác.");
        }

        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        account.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveAsync(cancellationToken);

        _logger.LogInformation("Người dùng AccountId {AccountId} đã đổi mật khẩu thành công.", accountId);
    }

    public async Task SendEmailVerificationAsync(string email, string? clientBaseUrl = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new BusinessRuleViolationException("Vui lòng nhập địa chỉ email hợp lệ.");
        }

        var cleanEmail = email.Trim().ToLowerInvariant();

        var accounts = await _unitOfWork.AccountRepository.GetAllAsync(cancellationToken);
        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);

        var account = accounts.FirstOrDefault(a =>
            a.Username.ToLower() == cleanEmail ||
            profiles.Any(p => p.AccountId == a.AccountId && p.Email.ToLower() == cleanEmail));

        var profile = profiles.FirstOrDefault(p => account != null && p.AccountId == account.AccountId);
        var fullName = profile?.FullName ?? cleanEmail;

        var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToHexString(tokenBytes).ToLowerInvariant();

        var verifyEntry = new EmailVerificationEntry
        {
            AccountId = account?.AccountId ?? 0,
            Email = cleanEmail,
            Token = token,
            OtpCode = otpCode,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        var cacheExpiry = TimeSpan.FromHours(24);
        _cache.Set($"email_verify_token_{token}", verifyEntry, cacheExpiry);
        _cache.Set($"email_verify_otp_{cleanEmail}_{otpCode}", verifyEntry, cacheExpiry);
        _cache.Set($"email_verify_otp_direct_{otpCode}", verifyEntry, cacheExpiry);

        var baseUrl = clientBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = _configuration["FrontendUrl"] ?? "http://localhost:5173";
        }
        baseUrl = baseUrl.TrimEnd('/');
        var verifyLink = $"{baseUrl}/verify-email?token={token}&email={Uri.EscapeDataString(cleanEmail)}";

        try
        {
            await _emailService.SendTemplatedEmailAsync(
                toEmail: cleanEmail,
                toName: fullName,
                templateName: "EmailVerification.html",
                subject: "Xác thực email tài khoản - ETR Management",
                tokens: new Dictionary<string, string>
                {
                    ["FullName"] = fullName,
                    ["OtpCode"] = otpCode,
                    ["VerificationLink"] = verifyLink,
                    ["ExpiresInHours"] = "24"
                },
                cancellationToken: cancellationToken);

            _logger.LogInformation("Đã gửi email xác thực đến {Email} thành công.", cleanEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi email xác thực cho {Email}.", cleanEmail);
            throw new BusinessRuleViolationException("Không thể gửi email lúc này. Vui lòng kiểm tra lại dịch vụ email hệ thống hoặc thử lại sau.");
        }
    }

    public Task VerifyEmailAsync(string token, string? email = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new BusinessRuleViolationException("Mã xác thực email hoặc token không được để trống.");
        }

        var cleanToken = token.Trim();
        EmailVerificationEntry? entry = null;

        if (_cache.TryGetValue($"email_verify_token_{cleanToken.ToLowerInvariant()}", out EmailVerificationEntry? tokenEntry))
        {
            entry = tokenEntry;
        }
        else if (!string.IsNullOrWhiteSpace(email) &&
                 _cache.TryGetValue($"email_verify_otp_{email.Trim().ToLowerInvariant()}_{cleanToken}", out EmailVerificationEntry? otpEmailEntry))
        {
            entry = otpEmailEntry;
        }
        else if (_cache.TryGetValue($"email_verify_otp_direct_{cleanToken}", out EmailVerificationEntry? otpDirectEntry))
        {
            entry = otpDirectEntry;
        }

        if (entry == null || entry.ExpiresAt < DateTime.UtcNow)
        {
            throw new BusinessRuleViolationException("Mã xác thực email không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu gửi lại mã mới.");
        }

        // Đánh dấu email đã được xác thực
        _cache.Set($"email_verified_{entry.Email}", true, TimeSpan.FromDays(365));

        // Hủy token
        _cache.Remove($"email_verify_token_{entry.Token}");
        _cache.Remove($"email_verify_otp_{entry.Email}_{entry.OtpCode}");
        _cache.Remove($"email_verify_otp_direct_{entry.OtpCode}");

        _logger.LogInformation("Xác thực email thành công cho {Email}.", entry.Email);
        return Task.CompletedTask;
    }

    public bool IsEmailVerified(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var cleanEmail = email.Trim().ToLowerInvariant();
        return _cache.TryGetValue($"email_verified_{cleanEmail}", out bool verified) && verified;
    }

    private class PasswordResetEntry
    {
        public int AccountId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string CurrentPasswordHash { get; set; } = string.Empty;
    }

    private class EmailVerificationEntry
    {
        public int AccountId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
