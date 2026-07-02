namespace ETR.Application.DTOs;

public record EtrSummaryResponse(
    int EtrRecordId,
    string Status,
    bool IsLocked,
    int TotalItems,
    int CompletedItems,
    double ProgressPercentage);
