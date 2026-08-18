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
