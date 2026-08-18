using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record ClassSearchResultResponse(
    int ClassId,
    string ClassCode,
    string ClassName,
    string CourseCode,
    string CourseName,
    ClassStatus Status);
