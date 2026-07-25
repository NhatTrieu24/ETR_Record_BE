namespace ETR.Application.DTOs;

public record ExpiringStudentResponse(
    int AccountId,
    string Email,
    string FullName,
    int CourseId,
    int ETRCourseRecordId,
    DateTime? ExpiryDate,
    string ValidityStatus
);
