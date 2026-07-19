namespace ETR.Application.DTOs.PracticalChecklistResult;

public record PracticalChecklistResultResponse(
    int PracticalChecklistResultId,
    int? SessionId,
    int SubjectResultId,
    int PracticalChecklistId,
    bool IsCompleted,
    int? VerifiedByAccountId,
    DateTime? CompletedAt,
    string? VerificationComment
);
