namespace ETR.Application.DTOs;

public record ClassSearchResultResponse(
    int ClassId,
    string ClassCode,
    string ClassName,
    string CourseCode,
    string CourseName,
    string Status);
