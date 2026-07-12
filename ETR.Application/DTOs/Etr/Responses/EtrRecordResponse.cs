namespace ETR.Application.DTOs;

public record EtrRecordResponse(
    int ETRCourseRecordId,
    int EnrollmentId,
    string Status,
    bool IsLocked,
    DateTime? SubmittedAt,
    DateTime? VerifiedAt,
    DateTime? CompletedAt);
