namespace ETR.Application.DTOs.Import;

/// <summary>One row parsed from a bulk student-account creation import Excel file (for Academic role, role is implicitly Student).</summary>
public record StudentAccountImportRow(
    int RowNumber,
    string Username,
    string Password,
    string DepartmentName,
    string FullName,
    DateTime? DateOfBirth = null,
    string? Gender = null,
    string? Phone = null,
    string? Organization = null);
