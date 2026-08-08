namespace ETR.Application.DTOs.Amendment;

public record AmendmentRequestResponse(
    int AmendmentRequestId,
    int SubjectResultId,
    int RequestedByAccountId,
    string Reason,
    string OldValue,
    string? NewValue,
    string Status,
    int? ApprovedByAccountId,
    DateTime? ApprovedAt,
    string? DecisionComment,
    DateTime CreatedAt
);
