namespace ETR.Application.DTOs;

public record StudentEtrStatusResponse(
    int CourseId,
    string CourseName,
    int ETRCourseRecordId,
    DateTime? IssuedDate,
    DateTime? ExpiryDate,
    string ValidityStatus
);
