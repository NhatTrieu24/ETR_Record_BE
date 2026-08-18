using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record CreateEnrollmentResponse(
    int EnrollmentId,
    int AccountId,
    int ClassId,
    EnrollmentStatus Status,
    DateTime EnrolledAt,
    int EtrCourseRecordId,
    EtrStatus EtrStatus,
    bool EtrIsLocked);
