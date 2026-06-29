namespace ETR.Application.DTOs;

public record UpdateEnrollmentRequest(
    int EnrollmentId,
    int LearnerId,
    int ClassId,
    string Status,
    DateTime EnrolledAt);
