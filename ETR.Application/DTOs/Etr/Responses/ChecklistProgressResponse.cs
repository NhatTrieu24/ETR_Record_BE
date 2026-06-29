namespace ETR.Application.DTOs;

public record ChecklistProgressResponse(
    int ProgressId,
    int ETRRecordId,
    int ChecklistItemId,
    bool IsCompleted,
    int? VerifiedBy,
    DateTime? CompletedAt,
    string? VerificationComment);
