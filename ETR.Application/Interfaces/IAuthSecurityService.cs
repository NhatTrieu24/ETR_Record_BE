using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IAuthSecurityService
{
    Task<AuthResponse> AuthenticateAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task ForgotPasswordAsync(string email, string? clientBaseUrl = null, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(string token, string newPassword, string? email = null, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(int accountId, string oldPassword, string newPassword, CancellationToken cancellationToken = default);
    Task SendEmailVerificationAsync(string email, string? clientBaseUrl = null, CancellationToken cancellationToken = default);
    Task VerifyEmailAsync(string token, string? email = null, CancellationToken cancellationToken = default);
    bool IsEmailVerified(string email);
}
