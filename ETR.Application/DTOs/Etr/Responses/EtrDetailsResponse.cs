namespace ETR.Application.DTOs;

public record EtrDetailsResponse(
    int ETRCourseRecordId,
    int EnrollmentId,
    string Status,
    bool IsLocked,
    DateTime? SubmittedAt,
    DateTime? VerifiedAt,
    DateTime? CompletedAt,
    IEnumerable<SubjectResultResponse> SubjectResults);
