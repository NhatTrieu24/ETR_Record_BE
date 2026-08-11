namespace ETR.Application.DTOs;

public record CourseSubjectResponse(
    int CourseId,
    int SubjectId,
    int SequenceNo,
    int RequiredHours,
    int RequiredSessions,
    bool IsMandatory,
    decimal PassingScore
);
