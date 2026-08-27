namespace ETR.Application.DTOs.Import;

/// <summary>
/// One row parsed from the "Students" sheet of a bulk class+roster import Excel file — assigns an
/// existing Student account to a class (either one being created in the same file's "Classes"
/// sheet, or an already-existing class referenced by ClassCode).
/// </summary>
public record StudentRosterImportRow(
    int RowNumber,
    string ClassCode,
    string Username);
