namespace ETR.Application.DTOs;

public record EnrollmentResponse(
    int EnrollmentId,
    int LearnerId,
    int ClassId,
    string Status,
    DateTime EnrolledAt);
