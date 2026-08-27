namespace ETR.Application.DTOs.Import;

/// <summary>One row parsed from the "Classes" sheet of a bulk class+roster import Excel file.</summary>
public record ClassImportRow(
    int RowNumber,
    string ClassCode,
    string ClassName,
    string CourseCode,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Location,
    int Capacity,
    string Status);
