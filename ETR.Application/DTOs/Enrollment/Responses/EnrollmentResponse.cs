using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record EnrollmentResponse(
    int EnrollmentId,
    int AccountId,
    int ClassId,
    EnrollmentStatus Status,
    DateTime EnrolledAt);
