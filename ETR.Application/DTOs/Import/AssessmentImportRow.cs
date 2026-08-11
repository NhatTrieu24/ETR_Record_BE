namespace ETR.Application.DTOs.Import;

/// <summary>One row parsed from an assessment score import Excel file.</summary>
public record AssessmentImportRow(
    int RowNumber,
    int AccountId,
    int SubjectResultId,
    decimal Score,
    string? Remark);
