namespace ETR.Application.DTOs;

public record EtrSearchResultResponse(
    int ETRCourseRecordId,
    string Status,
    string StudentName,
    string ClassCode,
    string ClassName,
    string CourseCode,
    string CourseName);
