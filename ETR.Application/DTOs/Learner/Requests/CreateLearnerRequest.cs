namespace ETR.Application.DTOs;

public record CreateLearnerRequest(
    string LearnerCode,
    string FullName,
    DateTime DateOfBirth,
    string Gender,
    string? Phone,
    string? Email,
    string IdentificationNumber,
    string? Organization,
    string Status,
    int LearnerTypeId);
