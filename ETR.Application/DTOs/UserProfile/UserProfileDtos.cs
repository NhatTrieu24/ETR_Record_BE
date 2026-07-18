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
    int? LearnerTypeId);

public record CreateUserProfileRequest(
    string UserCode,
    string FullName,
    string Email,
    string? Phone,
    DateTime DateOfBirth,
    string Gender,
    string? Organization,
    int? LearnerTypeId);

public record UpdateUserProfileRequest(
    string FullName,
    string Email,
    string? Phone,
    DateTime DateOfBirth,
    string Gender,
    string? Organization,
    int? LearnerTypeId);
