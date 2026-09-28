using System.ComponentModel.DataAnnotations;
using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record UserProfileResponse(
    int AccountId,
    string UserCode,
    string FullName,
    string Email,
    string? Phone,
    DateTime DateOfBirth,
    string Gender,
    string? Organization,
    LearnerStatus Status);

// Grounded is deliberately excluded here (see LearnerStatus enum docs — it is set/cleared only by
// CertificateValidityCalculator consumers, never by a plain profile edit); the service rejects it.
public record UpdateUserProfileStatusRequest(
    [Required] LearnerStatus Status);

public record CreateUserProfileRequest(
    string? UserCode,
    [Required, MaxLength(255), RegularExpression(@"^[\p{L}\s]+$", ErrorMessage = "Họ và tên chỉ được chứa chữ cái và khoảng trắng, không được chứa số hoặc ký tự đặc biệt.")] string FullName,
    [Required, EmailAddress, MaxLength(255), RegularExpression(@"^[a-zA-Z][a-zA-Z0-9._%+-]*@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Email phải là địa chỉ email hợp lệ và bắt đầu bằng chữ cái.")] string Email,
    [RegularExpression(@"^0[0-9]{9,10}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng số 0 và gồm 10-11 chữ số, không chứa ký tự đặc biệt.")] string? Phone,
    DateTime DateOfBirth,
    string Gender,
    string? Organization);

public record UpdateUserProfileRequest(
    [Required, MaxLength(255), RegularExpression(@"^[\p{L}\s]+$", ErrorMessage = "Họ và tên chỉ được chứa chữ cái và khoảng trắng, không được chứa số hoặc ký tự đặc biệt.")] string FullName,
    [Required, EmailAddress, MaxLength(255), RegularExpression(@"^[a-zA-Z][a-zA-Z0-9._%+-]*@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Email phải là địa chỉ email hợp lệ và bắt đầu bằng chữ cái.")] string Email,
    [RegularExpression(@"^0[0-9]{9,10}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng số 0 và gồm 10-11 chữ số, không chứa ký tự đặc biệt.")] string? Phone,
    DateTime DateOfBirth,
    string Gender,
    string? Organization);
