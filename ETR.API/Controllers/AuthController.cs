using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Identity &amp; Access Management
/// [Core Responsibility]: Authenticates users and generates JWT tokens based on Account credentials.
/// [Target Audience]: Public (Unauthenticated), All Roles
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public AuthController(IUnitOfWork unitOfWork, ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var accounts = await _unitOfWork.AccountRepository.GetAllAsync(cancellationToken);
        var account = accounts.FirstOrDefault(a => a.Username == request.Username && a.PasswordHash == request.Password);

        if (account == null || account.Status != "Active")
        {
            return Unauthorized("Invalid credentials or account is inactive.");
        }

        var roles = await _unitOfWork.RoleRepository.GetAllAsync(cancellationToken);
        var role = roles.FirstOrDefault(r => r.RoleId == account.RoleId);
        var roleName = role?.RoleName ?? "User";

        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        var profile = profiles.FirstOrDefault(p => p.AccountId == account.AccountId);

        var token = _tokenService.GenerateToken(account, role!);

        return Ok(new AuthResponse(account.AccountId, account.Username, profile?.FullName ?? "Unknown", token, "mock-refresh-token"));
    }

    [HttpPost("google-login")]
    public ActionResult GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        return Ok(new AuthResponse(99, "google_user", "Google User", "mock-google-jwt-token", "mock-google-refresh-token"));
    }

    [HttpPost("refresh-token")]
    public ActionResult RefreshToken([FromBody] RefreshTokenRequest request)
    {
        return Ok(new AuthResponse(1, "instructor_john", "John Doe", "new-mock-jwt-token", "new-mock-refresh-token"));
    }

    [HttpPost("logout")]
    public ActionResult Logout()
    {
        return Ok(new { message = "Đã đăng xuất thành công." });
    }

    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        return Ok("Đổi mật khẩu thành công (mock).");
    }

    [HttpPost("forgot-password")]
    public ActionResult ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        return Ok(new { message = "Yêu cầu đặt lại mật khẩu đã được gửi đến email: " + request.Email });
    }

    [HttpPost("reset-password")]
    public ActionResult ResetPassword([FromBody] ResetPasswordRequest request)
    {
        return Ok(new { message = "Mật khẩu đã được đặt lại thành công." });
    }

    [HttpGet("me")]
    public async Task<ActionResult> GetMe([FromQuery] int accountId, CancellationToken cancellationToken)
    {
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
