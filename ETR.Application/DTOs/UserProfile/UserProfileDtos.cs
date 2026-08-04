using System.ComponentModel.DataAnnotations;

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
    string Status);

public record UpdateUserProfileStatusRequest(
    [Required, RegularExpression("^(Active|Withdrawn|Graduated)$", ErrorMessage = "Status must be one of: Active, Withdrawn, Graduated.")]
    string Status);

public record CreateUserProfileRequest(
    string? UserCode,
    string FullName,
    string Email,
    string? Phone,
    DateTime DateOfBirth,
    string Gender,
    string? Organization);

public record UpdateUserProfileRequest(
    string FullName,
    string Email,
    string? Phone,
    DateTime DateOfBirth,
    string Gender,
    string? Organization);
