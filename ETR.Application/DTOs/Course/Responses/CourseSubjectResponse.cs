namespace ETR.Application.DTOs;

public record CourseSubjectResponse(
    int CourseId,
    int SubjectId,
    int SequenceNo,
    int RequiredHours,
    bool IsMandatory,
    decimal PassingScore
);
