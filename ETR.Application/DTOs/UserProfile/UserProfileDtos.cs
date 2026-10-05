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
    LearnerStatus Status,
    string? LicenseType = null,
    string? LicenseNumber = null,
    DateTime? LicenseExpiryDate = null,
    string? MedicalClass = null,
    DateTime? MedicalExpiryDate = null,
    int? IcaoElpLevel = null,
    DateTime? IcaoElpExpiryDate = null,
    string? TypeRatings = null,
    bool IsCredentialsVerified = false,
    int? CredentialsVerifiedByAccountId = null,
    DateTime? CredentialsVerifiedAt = null,
    int? DepartmentId = null,
    string? DepartmentName = null);

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
    string? Organization,
    string? LicenseType = null,
    string? LicenseNumber = null,
    DateTime? LicenseExpiryDate = null,
    string? MedicalClass = null,
    DateTime? MedicalExpiryDate = null,
    int? IcaoElpLevel = null,
    DateTime? IcaoElpExpiryDate = null,
    string? TypeRatings = null);

public record UpdateUserProfileRequest(
    [Required, MaxLength(255), RegularExpression(@"^[\p{L}\s]+$", ErrorMessage = "Họ và tên chỉ được chứa chữ cái và khoảng trắng, không được chứa số hoặc ký tự đặc biệt.")] string FullName,
    [Required, EmailAddress, MaxLength(255), RegularExpression(@"^[a-zA-Z][a-zA-Z0-9._%+-]*@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Email phải là địa chỉ email hợp lệ và bắt đầu bằng chữ cái.")] string Email,
    [RegularExpression(@"^0[0-9]{9,10}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng số 0 và gồm 10-11 chữ số, không chứa ký tự đặc biệt.")] string? Phone,
    DateTime DateOfBirth,
    string Gender,
    string? Organization,
    string? LicenseType = null,
    string? LicenseNumber = null,
    DateTime? LicenseExpiryDate = null,
    string? MedicalClass = null,
    DateTime? MedicalExpiryDate = null,
    int? IcaoElpLevel = null,
    DateTime? IcaoElpExpiryDate = null,
    string? TypeRatings = null);

public record UpdatePilotCredentialsRequest(
    string? LicenseType,
    string? LicenseNumber,
    DateTime? LicenseExpiryDate,
    string? MedicalClass,
    DateTime? MedicalExpiryDate,
    [Range(1, 6, ErrorMessage = "ICAO ELP Level phải nằm trong khoảng từ 1 đến 6.")] int? IcaoElpLevel,
    DateTime? IcaoElpExpiryDate,
    string? TypeRatings);

public record VerifyPilotCredentialsRequest(
    [Required] bool IsVerified,
    string? Comment = null,
    string? VerificationMethod = null,
    List<int>? ReviewedAttachmentIds = null);

public record CredentialAttachmentDto(
    int AttachmentId,
    int AccountId,
    string? DocType,
    string FileName,
    string Url,
    string? MimeType,
    long? FileSize,
    DateTime UploadedAt,
    int UploadedByAccountId);

public record UploadCredentialAttachmentRequest(
    [Required] string DocType,
    [Required] string Url,
    [Required] string FileName,
    string? PublicId,
    string? MimeType,
    long? FileSize);
