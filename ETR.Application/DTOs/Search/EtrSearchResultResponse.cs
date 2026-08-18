using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record EtrSearchResultResponse(
    int ETRCourseRecordId,
    EtrStatus Status,
    string StudentName,
    string ClassCode,
    string ClassName,
    string CourseCode,
    string CourseName);
