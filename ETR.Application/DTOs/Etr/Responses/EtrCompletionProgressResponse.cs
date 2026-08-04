namespace ETR.Application.DTOs;

public record CompletionCheckItem(string Name, bool IsMandatory, bool IsMet, string? Detail);

// M8: lets a Student/Instructor see submit-readiness BEFORE calling Submit and hitting a 400 —
// mirrors EtrService.SubmitEtrAsync's pre-validation checks exactly, but collects pass/fail
// instead of throwing on the first failure.
public record EtrCompletionProgressResponse(
    int ETRCourseRecordId,
    int TotalChecks,
    int MetChecks,
    decimal PercentComplete,
    IEnumerable<CompletionCheckItem> Checks);
