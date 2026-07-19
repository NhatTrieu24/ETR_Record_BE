namespace ETR.Application.DTOs;

public record UpdateAssessmentResultRequest(
    decimal Score,
    string? Remark
);
