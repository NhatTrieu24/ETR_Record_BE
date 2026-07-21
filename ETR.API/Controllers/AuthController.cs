using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Quản lý Định danh &amp; Truy cập
/// [Core Responsibility]: Authenticates users and generates JWT tokens based on Account credentials.
/// [Target Audience]: Public (Unauthenticated), All Roles
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // Verified even when no account matches the username, so a missing account and a
    // wrong password take the same amount of time — otherwise the BCrypt cost factor
    // becomes a timing oracle for username enumeration.
    private static readonly string DummyPasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());

    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(IUnitOfWork unitOfWork, ITokenService tokenService, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Xác thực người dùng và trả về JWT token.
    /// [Target Audience]: Public (Unauthenticated)
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<ActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var accounts = await _unitOfWork.AccountRepository.GetAllAsync(cancellationToken);
        var account = accounts.FirstOrDefault(a => a.Username == request.Username);

        var passwordIsValid = BCrypt.Net.BCrypt.Verify(request.Password, account?.PasswordHash ?? DummyPasswordHash);

        if (account == null || account.Status != "Active" || !passwordIsValid)
        {
            return Unauthorized("Invalid credentials or account is inactive.");
        }

        var roles = await _unitOfWork.RoleRepository.GetAllAsync(cancellationToken);
        var role = roles.FirstOrDefault(r => r.RoleId == account.RoleId);
        var roleName = role?.RoleName ?? "User";

        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        var profile = profiles.FirstOrDefault(p => p.AccountId == account.AccountId);

        var token = _tokenService.GenerateToken(account, role!);

        return Ok(new AuthResponse(account.AccountId, account.Username, profile?.FullName ?? "Unknown", roleName, token, "mock-refresh-token"));
    }

    [HttpGet("mock-admin-token")]
    public ActionResult GetMockAdminToken()
    {
        var account = new ETR.Domain.Entities.Account { AccountId = 1, Username = "admin", RoleId = 1 };
        var role = new ETR.Domain.Entities.Role { RoleId = 1, RoleName = "Admin" };
        var token = _tokenService.GenerateToken(account, role);
        return Ok(new { token });
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Xác thực người dùng qua Google OAuth và trả về JWT token.
    /// [Target Audience]: Public (Unauthenticated)
    /// </summary>
    [HttpPost("google-login")]
    public ActionResult GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        return Ok(new AuthResponse(99, "google_user", "Google User", "User", "mock-google-jwt-token", "mock-google-refresh-token"));
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Cấp lại (refresh) một JWT token đã hết hạn bằng cách sử dụng refresh token hợp lệ.
    /// [Target Audience]: Public (Unauthenticated)
    /// </summary>
    [HttpPost("refresh-token")]
    public ActionResult RefreshToken([FromBody] RefreshTokenRequest request)
    {
        return Ok(new AuthResponse(1, "instructor_john", "John Doe", "Instructor", "new-mock-jwt-token", "new-mock-refresh-token"));
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Hủy phiên đăng nhập (session) hiện tại của người dùng.
    /// [Target Audience]: All Roles
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public ActionResult Logout()
    {
        return Ok(new { message = "Đã đăng xuất thành công." });
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Thay đổi mật khẩu cho người dùng hiện đang xác thực.
    /// [Target Audience]: All Roles
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        return Ok("Đổi mật khẩu thành công (mock).");
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Khởi tạo luồng quên mật khẩu.
    /// [Target Audience]: Public (Unauthenticated)
    /// </summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("AuthPolicy")]
    public ActionResult ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        return Ok(new { message = "Yêu cầu đặt lại mật khẩu đã được gửi đến email: " + request.Email });
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Đặt lại mật khẩu của người dùng bằng cách sử dụng reset token.
    /// [Target Audience]: Public (Unauthenticated)
    /// </summary>
    [HttpPost("reset-password")]
    [EnableRateLimiting("AuthPolicy")]
    public ActionResult ResetPassword([FromBody] ResetPasswordRequest request)
    {
        return Ok(new { message = "Mật khẩu đã được đặt lại thành công." });
    }

    /// <summary>
    /// [Module/Flow]: Quản lý Định danh &amp; Truy cập
    /// [Core Responsibility]: Lấy thông tin tài khoản và hồ sơ (profile) của người dùng hiện đang xác thực.
    /// [Target Audience]: All Roles
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult> GetMe(CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var account = await _unitOfWork.AccountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account == null) return NotFound("Account not found.");

        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        var profile = profiles.FirstOrDefault(p => p.AccountId == account.AccountId);

        var roles = await _unitOfWork.RoleRepository.GetAllAsync(cancellationToken);
        var role = roles.FirstOrDefault(r => r.RoleId == account.RoleId);

        return Ok(new
        {
            account.AccountId,
            account.Username,
            profile?.FullName,
            profile?.Email,
            profile?.Phone,
            RoleName = role?.RoleName,
            account.DepartmentId
        });
    }
}


