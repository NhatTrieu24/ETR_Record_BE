namespace ETR.Application.DTOs.Import;

public record ImportValidationResult(
    int TotalRows,
    int ValidRows,
    int ErrorRows,
    bool CanCommit,
    List<ImportRowError> Errors);
