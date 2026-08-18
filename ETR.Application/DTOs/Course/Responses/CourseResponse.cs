using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record CourseResponse(
    int CourseId,
    string CourseCode,
    string CourseName,
    string Description,
    int DurationHours,
    CourseStatus Status,
    int? ValidityMonths = null,
    string? CourseType = null,
    List<CourseSubjectResponse>? Subjects = null,
    int VersionNo = 1
);
