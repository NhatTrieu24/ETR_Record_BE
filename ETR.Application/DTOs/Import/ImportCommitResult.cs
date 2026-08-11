namespace ETR.Application.DTOs.Import;

public record ImportCommitResult(
    int Imported,
    int Skipped,
    List<ImportRowError> Errors);
