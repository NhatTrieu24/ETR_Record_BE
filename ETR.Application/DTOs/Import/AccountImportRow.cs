namespace ETR.Application.DTOs.Import;

/// <summary>One row parsed from a bulk user-account creation import Excel file.</summary>
public record AccountImportRow(
    int RowNumber,
    string Username,
    string Password,
    string RoleName,
    string DepartmentName,
    string FullName,
    DateTime? DateOfBirth = null,
    string? Gender = null,
    string? Phone = null,
    string? Organization = null);
