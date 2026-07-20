namespace ETR.Application.DTOs.PracticalChecklistResult;

public record PracticalChecklistResultResponse(
    int PracticalChecklistResultId,
    int? SessionId,
    int SubjectResultId,
    int PracticalChecklistId,
    decimal Score,
    string ResultStatus,
    int? VerifiedByAccountId,
    DateTime? CompletedAt,
    string? VerificationComment,
    bool IsPublished,
    DateTime? PublishedAt,
    int? AccountId
);
