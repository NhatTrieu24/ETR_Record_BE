using ETR.Application.DTOs.Import;

namespace ETR.Application.Interfaces;

public interface IImportService
{
    // ── Attendance ──────────────────────────────────────────────────────────
    Task<byte[]> GenerateAttendanceTemplateAsync(int sessionId, CancellationToken ct = default);
    Task<ImportValidationResult> ValidateAttendanceImportAsync(int sessionId, Stream fileStream, CancellationToken ct = default);
    Task<ImportCommitResult> CommitAttendanceImportAsync(int sessionId, Stream fileStream, int recordedByAccountId, string? recordedByRoleName, CancellationToken ct = default);

    // ── Assessment (Theory / Practical) ────────────────────────────────────
    Task<byte[]> GenerateAssessmentTemplateAsync(int assessmentId, CancellationToken ct = default);
    Task<ImportValidationResult> ValidateAssessmentImportAsync(int assessmentId, Stream fileStream, CancellationToken ct = default);
    Task<ImportCommitResult> CommitAssessmentImportAsync(int assessmentId, Stream fileStream, int gradedByAccountId, string? gradedByRoleName, CancellationToken ct = default);

    // ── Accounts (bulk user creation) ───────────────────────────────────────
    Task<byte[]> GenerateAccountImportTemplateAsync(CancellationToken ct = default);
    Task<ImportValidationResult> ValidateAccountImportAsync(Stream fileStream, bool isCallerAdmin, CancellationToken ct = default);
    Task<ImportCommitResult> CommitAccountImportAsync(Stream fileStream, int createdByAccountId, bool isCallerAdmin, CancellationToken ct = default);
}
