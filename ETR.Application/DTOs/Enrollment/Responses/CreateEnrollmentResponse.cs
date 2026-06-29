namespace ETR.Application.DTOs;

public record CreateEnrollmentResponse(
    int EnrollmentId,
    int LearnerId,
    int ClassId,
    string Status,
    DateTime EnrolledAt,
    int EtrRecordId,
    string EtrStatus,
    bool EtrIsLocked);
