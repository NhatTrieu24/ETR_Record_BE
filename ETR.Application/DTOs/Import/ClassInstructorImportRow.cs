namespace ETR.Application.DTOs.Import;

/// <summary>One row parsed from the "Instructors" sheet of a bulk class+roster import Excel file.</summary>
public record ClassInstructorImportRow(
    int RowNumber,
    string ClassCode,
    string SubjectCode,
    string Username);
