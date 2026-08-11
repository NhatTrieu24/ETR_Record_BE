namespace ETR.Application.DTOs.Import;

/// <summary>One row parsed from an attendance import Excel file.</summary>
public record AttendanceImportRow(
    int RowNumber,
    int EnrollmentId,
    string Status,
    string? Remarks);
