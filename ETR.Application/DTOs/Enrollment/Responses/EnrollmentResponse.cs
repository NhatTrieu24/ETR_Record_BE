namespace ETR.Application.DTOs;

public record EnrollmentResponse(
    int EnrollmentId,
    int AccountId,
    int ClassId,
    string Status,
    DateTime EnrolledAt);
