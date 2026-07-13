using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

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
        var users = await _unitOfWork.UserRepository.GetAllAsync(cancellationToken);
        var user = users.FirstOrDefault(u => 
            u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase) && 
            u.IsActive && 
            !u.IsDeleted);
        
        // Note: For a real app, hash the request.Password and compare with PasswordHash. 
        // Using plain comparison here based on existing code structure
        if (user == null || user.PasswordHash != request.Password)
        {
            return Unauthorized("Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        var role = await _unitOfWork.RoleRepository.GetByIdAsync(user.RoleId, cancellationToken);
        if (role == null)
        {
            return Unauthorized("Tài khoản không được cấu hình quyền hạn hợp lệ.");
        }

        var token = _tokenService.GenerateToken(user, role);
        return Ok(new LoginResponseDto(user.UserId, user.Username, user.FullName, role.RoleName, token));
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
        var user = await _unitOfWork.UserRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null) return NotFound("Không tìm thấy người dùng.");

        user.PasswordHash = request.NewPassword;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = request.UserId;

        _unitOfWork.UserRepository.Update(user);
        await _unitOfWork.SaveAsync(cancellationToken);

        return Ok("Đổi mật khẩu thành công.");
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
    public async Task<ActionResult> GetMe([FromQuery] int userId, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.UserRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null) return NotFound("Chưa đăng nhập hoặc không tìm thấy người dùng.");
        return Ok(user);
    }
}
