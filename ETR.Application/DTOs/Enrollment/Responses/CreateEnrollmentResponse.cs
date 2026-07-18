namespace ETR.Application.DTOs;

public record CreateEnrollmentResponse(
    int EnrollmentId,
    int AccountId,
    int ClassId,
    string Status,
    DateTime EnrolledAt,
    int EtrCourseRecordId,
    string EtrStatus,
    bool EtrIsLocked);
