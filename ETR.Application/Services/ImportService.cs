using ClosedXML.Excel;
using ETR.Application.Compliance;
using ETR.Application.DTOs.Import;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Domain.Enums;

namespace ETR.Application.Services;

public class ImportService : IImportService
{
    private readonly IUnitOfWork _unitOfWork;

    // Column indices (1-based, matches Excel columns A=1, B=2 …)
    private const int AttColEnrollmentId = 1;
    private const int AttColFullName     = 2;
    private const int AttColUserCode     = 3;
    private const int AttColStatus       = 4;
    private const int AttColRemarks      = 5;
    private const int AttDataStartRow    = 4; // rows 1-3 are title / metadata / header

    private const int AsmColAccountId      = 1;
    private const int AsmColFullName        = 2;
    private const int AsmColUserCode        = 3;
    private const int AsmColSubjectResultId = 4;
    private const int AsmColScore           = 5;
    private const int AsmColRemark          = 6;
    private const int AsmDataStartRow       = 4;

    private static readonly string[] ValidAttendanceStatuses = ["Present", "Absent"];

    private const int AccColUsername       = 1;
    private const int AccColPassword       = 2;
    private const int AccColRoleName       = 3;
    private const int AccColDepartmentName = 4;
    private const int AccDataStartRow      = 3; // row 1 is title, row 2 is header

    public ImportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  ATTENDANCE
    // ════════════════════════════════════════════════════════════════════════

    public async Task<byte[]> GenerateAttendanceTemplateAsync(int sessionId, CancellationToken ct = default)
    {
        var session = await _unitOfWork.SessionRepository.GetByIdAsync(sessionId, ct)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(session.ClassId, ct)
            ?? throw new KeyNotFoundException($"Class {session.ClassId} not found.");

        var subject = await _unitOfWork.SubjectRepository.GetByIdAsync(session.SubjectId, ct);

        var enrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(ct))
            .Where(e => e.ClassId == session.ClassId && !e.IsDeleted && e.Status == EnrollmentStatus.Active)
            .ToList();

        var profiles = (await _unitOfWork.UserProfileRepository.GetAllAsync(ct))
            .Where(p => enrollments.Select(e => e.AccountId).Contains(p.AccountId))
            .ToDictionary(p => p.AccountId);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Điểm danh");

        // ── Row 1: title ──────────────────────────────────────────────────
        ws.Cell(1, 1).Value = $"BẢNG ĐIỂM DANH - {session.SessionTitle} - {trainingClass.ClassCode}";
        ws.Range(1, 1, 1, AttColRemarks).Merge().Style
            .Font.SetBold(true)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        // ── Row 2: metadata ───────────────────────────────────────────────
        ws.Cell(2, 1).Value = $"SessionId: {session.SessionId}";
        ws.Cell(2, 2).Value = $"ClassId: {session.ClassId}";
        ws.Cell(2, 3).Value = $"SubjectId: {session.SubjectId}";
        ws.Cell(2, 4).Value = $"Ngày: {(session.SessionDate.HasValue ? session.SessionDate.Value.ToString("dd/MM/yyyy") : "TBA")}";
        ws.Cell(2, 5).Value = $"Môn: {subject?.SubjectName ?? session.SubjectId.ToString()}";

        // ── Row 3: column headers ─────────────────────────────────────────
        ws.Cell(3, AttColEnrollmentId).Value = "EnrollmentId";
        ws.Cell(3, AttColFullName).Value     = "Họ và tên";
        ws.Cell(3, AttColUserCode).Value     = "Mã học viên";
        ws.Cell(3, AttColStatus).Value       = "Trạng thái (Present/Absent)*";
        ws.Cell(3, AttColRemarks).Value      = "Ghi chú";
        ws.Row(3).Style.Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.LightSteelBlue);

        // ── Data rows ─────────────────────────────────────────────────────
        int row = AttDataStartRow;
        foreach (var enrollment in enrollments.OrderBy(e => e.EnrollmentId))
        {
            profiles.TryGetValue(enrollment.AccountId, out var profile);
            ws.Cell(row, AttColEnrollmentId).Value = enrollment.EnrollmentId;
            ws.Cell(row, AttColFullName).Value     = profile?.FullName ?? string.Empty;
            ws.Cell(row, AttColUserCode).Value     = profile?.UserCode ?? string.Empty;
            ws.Cell(row, AttColStatus).Value       = string.Empty; // filled by user
            ws.Cell(row, AttColRemarks).Value      = string.Empty;
            row++;
        }

        // Lock read-only columns with light styling
        ws.Column(AttColEnrollmentId).Style.Fill.SetBackgroundColor(XLColor.LightGray);
        ws.Column(AttColFullName).Style.Fill.SetBackgroundColor(XLColor.LightGray);
        ws.Column(AttColUserCode).Style.Fill.SetBackgroundColor(XLColor.LightGray);
        ws.Columns().AdjustToContents();

        // Dropdown validation for Status column
        var statusRange = ws.Range(AttDataStartRow, AttColStatus, row - 1, AttColStatus);
        statusRange.CreateDataValidation().List("\"Present,Absent\"", true);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<ImportValidationResult> ValidateAttendanceImportAsync(int sessionId, Stream fileStream, CancellationToken ct = default)
    {
        var rows = await ParseAttendanceRowsAsync(fileStream, ct);
        var errors = await ValidateAttendanceRowsAsync(sessionId, rows, ct);
        return new ImportValidationResult(
            TotalRows: rows.Count,
            ValidRows: rows.Count - errors.Select(e => e.Row).Distinct().Count(),
            ErrorRows: errors.Select(e => e.Row).Distinct().Count(),
            CanCommit: errors.Count == 0,
            Errors: errors);
    }

    public async Task<ImportCommitResult> CommitAttendanceImportAsync(
        int sessionId, Stream fileStream,
        int recordedByAccountId, string? recordedByRoleName,
        CancellationToken ct = default)
    {
        var rows = await ParseAttendanceRowsAsync(fileStream, ct);
        var errors = await ValidateAttendanceRowsAsync(sessionId, rows, ct);
        if (errors.Count > 0)
            return new ImportCommitResult(Imported: 0, Skipped: rows.Count, Errors: errors);

        var session = await _unitOfWork.SessionRepository.GetByIdAsync(sessionId, ct)!;

        // Ownership check — one check for the whole batch (all rows → same session → same subject)
        var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(session!.ClassId, ct);
        var isAssigned = trainingClass != null && _unitOfWork.ClassSubjectRepository.GetQueryable()
            .Any(cs => cs.ClassId == trainingClass.ClassId
                    && cs.SubjectId == session.SubjectId
                    && cs.InstructorAccountId == recordedByAccountId);
        ClassOwnershipValidator.EnsureInstructorOwnsSubject(recordedByRoleName, isAssigned);

        var existingRecords = (await _unitOfWork.AttendanceRecordRepository.GetAllAsync(ct))
            .Where(r => r.SessionId == sessionId && !r.IsDeleted)
            .Select(r => r.EnrollmentId)
            .ToHashSet();

        var commitErrors = new List<ImportRowError>();
        int imported = 0, skipped = 0, updated = 0;

        return await _unitOfWork.ExecuteInStrategyAsync(async (innerCt) =>
        {
            await _unitOfWork.BeginTransactionAsync(innerCt);
            try
            {
                var allRecords = await _unitOfWork.AttendanceRecordRepository.GetAllAsync(innerCt);

                foreach (var row in rows)
                {
                    var existingRecord = allRecords.FirstOrDefault(r => r.SessionId == sessionId && r.EnrollmentId == row.EnrollmentId && !r.IsDeleted);
                    var newStatus = Enum.Parse<AttendanceStatus>(row.Status, ignoreCase: true);

                    if (existingRecord != null)
                    {
                        var oldStatus = existingRecord.Status;
                        existingRecord.Status = newStatus;
                        existingRecord.Remarks = row.Remarks;
                        existingRecord.UpdatedAt = DateTime.UtcNow;
                        existingRecord.UpdatedByAccountId = recordedByAccountId;

                        _unitOfWork.AttendanceRecordRepository.Update(existingRecord);
                        updated++;

                        if (oldStatus != newStatus)
                        {
                            await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                            {
                                AccountId = recordedByAccountId,
                                ActionType = AuditActionType.UPDATE.ToString(),
                                EntityName = "AttendanceRecord",
                                RecordId = existingRecord.AttendanceRecordId,
                                OldValue = oldStatus.ToString(),
                                NewValue = newStatus.ToString(),
                                Description = $"Import updated AttendanceRecord status from {oldStatus} to {newStatus}",
                                CreatedAt = DateTime.UtcNow
                            }, innerCt);
                        }
                    }
                    else
                    {
                        var record = new AttendanceRecord
                        {
                            SessionId            = sessionId,
                            EnrollmentId         = row.EnrollmentId,
                            Status               = newStatus,
                            Remarks              = row.Remarks,
                            RecordedByAccountId  = recordedByAccountId,
                            RecordedAt           = DateTime.UtcNow,
                            CreatedAt            = DateTime.UtcNow,
                            CreatedByAccountId   = recordedByAccountId
                        };
                        await _unitOfWork.AttendanceRecordRepository.AddAsync(record, innerCt);
                        imported++;
                    }
                }

                if (imported > 0 || updated > 0)
                {
                    await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                    {
                        AccountId = recordedByAccountId,
                        ActionType = AuditActionType.IMPORT_ATTENDANCE.ToString(),
                        EntityName = "AttendanceRecord",
                        RecordId = sessionId,
                        Description = $"Imported {imported} new and updated {updated} attendance records for session {sessionId}",
                        CreatedAt = DateTime.UtcNow
                    }, innerCt);
                }

                await _unitOfWork.SaveAsync(innerCt);

                // Recalculate AttendanceRate for each affected enrollment
                await RecalculateAttendanceRatesAsync(sessionId, session.SubjectId, session.ClassId, rows.Select(r => r.EnrollmentId), innerCt);

                await _unitOfWork.SaveAsync(innerCt);
                await _unitOfWork.CommitTransactionAsync(innerCt);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(innerCt);
                throw;
            }

            return new ImportCommitResult(Imported: imported, Skipped: skipped, Errors: commitErrors);
        }, ct);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  ASSESSMENT
    // ════════════════════════════════════════════════════════════════════════

    public async Task<byte[]> GenerateAssessmentTemplateAsync(int assessmentId, CancellationToken ct = default)
    {
        var assessment = await _unitOfWork.AssessmentRepository.GetByIdAsync(assessmentId, ct)
            ?? throw new KeyNotFoundException($"Assessment {assessmentId} not found.");

        var subject = await _unitOfWork.SubjectRepository.GetByIdAsync(assessment.SubjectId, ct);
        var course  = await _unitOfWork.CourseRepository.GetByIdAsync(assessment.CourseId, ct);

        // All active enrollments for this course
        var allClasses = (await _unitOfWork.ClassRepository.GetAllAsync(ct))
            .Where(c => c.CourseId == assessment.CourseId && !c.IsDeleted)
            .Select(c => c.ClassId).ToHashSet();

        var enrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(ct))
            .Where(e => allClasses.Contains(e.ClassId) && !e.IsDeleted && e.Status == EnrollmentStatus.Active)
            .ToList();

        var profiles = (await _unitOfWork.UserProfileRepository.GetAllAsync(ct))
            .Where(p => enrollments.Select(e => e.AccountId).Contains(p.AccountId))
            .ToDictionary(p => p.AccountId);

        // SubjectResult lookup: (etrId → subjectResultId) — need AccountId → ETR → SubjectResult
        var etrs = (await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(ct))
            .Where(e => enrollments.Select(en => en.EnrollmentId).Contains(e.EnrollmentId) && !e.IsDeleted)
            .ToDictionary(e => e.EnrollmentId);

        var subjectResults = (await _unitOfWork.SubjectResultRepository.GetAllAsync(ct))
            .Where(sr => sr.SubjectId == assessment.SubjectId && !sr.IsDeleted)
            .ToDictionary(sr => sr.EtrId);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Nhập điểm");

        // ── Row 1: title ──────────────────────────────────────────────────
        ws.Cell(1, 1).Value = $"BẢNG NHẬP ĐIỂM - {assessment.ComponentName} ({assessment.AssessmentType}) - Môn: {subject?.SubjectName ?? assessment.SubjectId.ToString()}";
        ws.Range(1, 1, 1, AsmColRemark).Merge().Style
            .Font.SetBold(true)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        // ── Row 2: metadata ───────────────────────────────────────────────
        ws.Cell(2, 1).Value = $"AssessmentId: {assessmentId}";
        ws.Cell(2, 2).Value = $"CourseId: {assessment.CourseId} ({course?.CourseName ?? string.Empty})";
        ws.Cell(2, 3).Value = $"PassingScore: {assessment.PassingScore}";
        ws.Cell(2, 4).Value = $"Weight: {assessment.Weight}";
        ws.Cell(2, 5).Value = $"Type: {assessment.AssessmentType}";

        // ── Row 3: column headers ─────────────────────────────────────────
        ws.Cell(3, AsmColAccountId).Value      = "AccountId";
        ws.Cell(3, AsmColFullName).Value        = "Họ và tên";
        ws.Cell(3, AsmColUserCode).Value        = "Mã học viên";
        ws.Cell(3, AsmColSubjectResultId).Value = "SubjectResultId";
        ws.Cell(3, AsmColScore).Value           = $"Điểm (0-100)*  [Đạt: ≥{assessment.PassingScore}]";
        ws.Cell(3, AsmColRemark).Value          = "Ghi chú";
        ws.Row(3).Style.Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.LightSteelBlue);

        // ── Data rows ─────────────────────────────────────────────────────
        int row = AsmDataStartRow;
        foreach (var enrollment in enrollments.OrderBy(e => e.EnrollmentId))
        {
            profiles.TryGetValue(enrollment.AccountId, out var profile);
            etrs.TryGetValue(enrollment.EnrollmentId, out var etr);
            int subjectResultId = 0;
            if (etr != null) subjectResults.TryGetValue(etr.ETRCourseRecordId, out var sr);
            if (etr != null && subjectResults.TryGetValue(etr.ETRCourseRecordId, out var srVal))
                subjectResultId = srVal.SubjectResultId;

            ws.Cell(row, AsmColAccountId).Value      = enrollment.AccountId;
            ws.Cell(row, AsmColFullName).Value        = profile?.FullName ?? string.Empty;
            ws.Cell(row, AsmColUserCode).Value        = profile?.UserCode ?? string.Empty;
            ws.Cell(row, AsmColSubjectResultId).Value = subjectResultId;
            ws.Cell(row, AsmColScore).Value           = string.Empty; // filled by user
            ws.Cell(row, AsmColRemark).Value          = string.Empty;
            row++;
        }

        ws.Column(AsmColAccountId).Style.Fill.SetBackgroundColor(XLColor.LightGray);
        ws.Column(AsmColFullName).Style.Fill.SetBackgroundColor(XLColor.LightGray);
        ws.Column(AsmColUserCode).Style.Fill.SetBackgroundColor(XLColor.LightGray);
        ws.Column(AsmColSubjectResultId).Style.Fill.SetBackgroundColor(XLColor.LightGray);
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<ImportValidationResult> ValidateAssessmentImportAsync(int assessmentId, Stream fileStream, CancellationToken ct = default)
    {
        var rows = await ParseAssessmentRowsAsync(fileStream, ct);
        var errors = await ValidateAssessmentRowsAsync(assessmentId, rows, ct);
        return new ImportValidationResult(
            TotalRows: rows.Count,
            ValidRows: rows.Count - errors.Select(e => e.Row).Distinct().Count(),
            ErrorRows: errors.Select(e => e.Row).Distinct().Count(),
            CanCommit: errors.Count == 0,
            Errors: errors);
    }

    public async Task<ImportCommitResult> CommitAssessmentImportAsync(
        int assessmentId, Stream fileStream,
        int gradedByAccountId, string? gradedByRoleName,
        CancellationToken ct = default)
    {
        var rows = await ParseAssessmentRowsAsync(fileStream, ct);
        var errors = await ValidateAssessmentRowsAsync(assessmentId, rows, ct);
        if (errors.Count > 0)
            return new ImportCommitResult(Imported: 0, Skipped: rows.Count, Errors: errors);

        var assessment = (await _unitOfWork.AssessmentRepository.GetByIdAsync(assessmentId, ct))!;

        // Ownership check — same assessment → same subject
        var allClasses = (await _unitOfWork.ClassRepository.GetAllAsync(ct))
            .Where(c => c.CourseId == assessment.CourseId && !c.IsDeleted).ToList();
        var isAssigned = allClasses.Any(c => _unitOfWork.ClassSubjectRepository.GetQueryable()
            .Any(cs => cs.ClassId == c.ClassId
                    && cs.SubjectId == assessment.SubjectId
                    && cs.InstructorAccountId == gradedByAccountId));
        ClassOwnershipValidator.EnsureInstructorOwnsSubject(gradedByRoleName, isAssigned);

        var existingResults = (await _unitOfWork.AssessmentResultRepository.GetAllAsync(ct))
            .Where(r => r.AssessmentId == assessmentId && !r.IsDeleted)
            .Select(r => r.AccountId)
            .ToHashSet();

        int imported = 0, skipped = 0;
        var commitErrors = new List<ImportRowError>();

        return await _unitOfWork.ExecuteInStrategyAsync(async (innerCt) =>
        {
            await _unitOfWork.BeginTransactionAsync(innerCt);
            try
            {
                var allResults = await _unitOfWork.AssessmentResultRepository.GetAllAsync(innerCt);

                foreach (var row in rows)
                {
                    var passSnapshot = assessment.PassingScore;
                    var weightSnapshot = assessment.Weight;

                    // Try to fill the Pending placeholder created at Enroll time
                    var pending = allResults.FirstOrDefault(r =>
                        r.AssessmentId == assessmentId &&
                        r.AccountId == row.AccountId &&
                        r.ResultStatus == "Pending" &&
                        r.SessionId == null &&
                        r.AttemptNo == 1);

                    if (pending != null)
                    {
                        var snap = pending.PassingScoreSnapshot ?? passSnapshot;
                        pending.Score               = row.Score;
                        pending.ResultStatus        = row.Score >= snap ? "Passed" : "Failed";
                        pending.GradedByAccountId   = gradedByAccountId;
                        pending.RecordedAt          = DateTime.UtcNow;
                        pending.TakenAt             = DateTime.UtcNow;
                        pending.Remark              = row.Remark;
                        pending.UpdatedAt           = DateTime.UtcNow;
                        pending.UpdatedByAccountId  = gradedByAccountId;
                        _unitOfWork.AssessmentResultRepository.Update(pending);
                    }
                    else if (!existingResults.Contains(row.AccountId))
                    {
                        var resultStatus = row.Score >= passSnapshot ? "Passed" : "Failed";
                        var result = new AssessmentResult
                        {
                            AssessmentId          = assessmentId,
                            AccountId             = row.AccountId,
                            SubjectResultId       = row.SubjectResultId,
                            Score                 = row.Score,
                            ResultStatus          = resultStatus,
                            GradedByAccountId     = gradedByAccountId,
                            RecordedAt            = DateTime.UtcNow,
                            TakenAt               = DateTime.UtcNow,
                            Remark                = row.Remark,
                            AttemptNo             = 1,
                            PassingScoreSnapshot  = passSnapshot,
                            WeightSnapshot        = weightSnapshot,
                            CreatedAt             = DateTime.UtcNow,
                            CreatedByAccountId    = gradedByAccountId
                        };
                        await _unitOfWork.AssessmentResultRepository.AddAsync(result, innerCt);
                    }
                    else
                    {
                        commitErrors.Add(new ImportRowError(row.RowNumber, "AccountId",
                            $"AccountId {row.AccountId} đã có điểm cho assessment này (không phải Pending). Dùng PUT để cập nhật."));
                        skipped++;
                        continue;
                    }
                    imported++;
                }

                if (imported > 0)
                {
                    await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                    {
                        AccountId = gradedByAccountId,
                        ActionType = AuditActionType.IMPORT_ASSESSMENT.ToString(),
                        EntityName = "AssessmentResult",
                        RecordId = assessmentId,
                        Description = $"Imported {imported} assessment results for assessment {assessmentId}",
                        CreatedAt = DateTime.UtcNow
                    }, innerCt);
                }

                await _unitOfWork.SaveAsync(innerCt);
                await _unitOfWork.CommitTransactionAsync(innerCt);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(innerCt);
                throw;
            }

            return new ImportCommitResult(Imported: imported, Skipped: skipped, Errors: commitErrors);
        }, ct);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ════════════════════════════════════════════════════════════════════════

    private static Task<List<AttendanceImportRow>> ParseAttendanceRowsAsync(Stream fileStream, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var workbook = new XLWorkbook(fileStream);
        var ws = workbook.Worksheet(1);
        var rows = new List<AttendanceImportRow>();
        int lastRow = ws.LastRowUsed()?.RowNumber() ?? AttDataStartRow - 1;

        for (int r = AttDataStartRow; r <= lastRow; r++)
        {
            var enrollmentIdCell = ws.Cell(r, AttColEnrollmentId).GetString().Trim();
            if (string.IsNullOrEmpty(enrollmentIdCell)) continue;

            if (!int.TryParse(enrollmentIdCell, out var enrollmentId)) continue;

            var status  = ws.Cell(r, AttColStatus).GetString().Trim();
            var remarks = ws.Cell(r, AttColRemarks).GetString().Trim();
            rows.Add(new AttendanceImportRow(r, enrollmentId, status, string.IsNullOrEmpty(remarks) ? null : remarks));
        }
        return Task.FromResult(rows);
    }

    private async Task<List<ImportRowError>> ValidateAttendanceRowsAsync(int sessionId, List<AttendanceImportRow> rows, CancellationToken ct)
    {
        var errors = new List<ImportRowError>();

        var session = await _unitOfWork.SessionRepository.GetByIdAsync(sessionId, ct);
        if (session == null)
        {
            errors.Add(new ImportRowError(0, "SessionId", $"Session {sessionId} không tồn tại."));
            return errors;
        }
        if (session.IsConfirmed)
        {
            errors.Add(new ImportRowError(0, "SessionId", "Session đã được confirm, không thể import điểm danh."));
            return errors;
        }

        var validEnrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(ct))
            .Where(e => e.ClassId == session.ClassId && !e.IsDeleted)
            .Select(e => e.EnrollmentId)
            .ToHashSet();

        var lockedEtrEnrollments = (await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(ct))
            .Where(e => validEnrollments.Contains(e.EnrollmentId) && e.IsLocked && !e.IsDeleted)
            .Select(e => e.EnrollmentId)
            .ToHashSet();

        var seenInFile = new HashSet<int>();
        foreach (var row in rows)
        {
            if (!ValidAttendanceStatuses.Contains(row.Status, StringComparer.OrdinalIgnoreCase))
                errors.Add(new ImportRowError(row.RowNumber, "Status",
                    $"Giá trị '{row.Status}' không hợp lệ. Chấp nhận: Present, Absent."));

            if (!validEnrollments.Contains(row.EnrollmentId))
                errors.Add(new ImportRowError(row.RowNumber, "EnrollmentId",
                    $"EnrollmentId {row.EnrollmentId} không thuộc lớp của session này."));

            if (lockedEtrEnrollments.Contains(row.EnrollmentId))
                errors.Add(new ImportRowError(row.RowNumber, "EnrollmentId",
                    $"Học viên này đã bị khóa ETR (ETR Locked), không thể thao tác."));

            if (!seenInFile.Add(row.EnrollmentId))
                errors.Add(new ImportRowError(row.RowNumber, "EnrollmentId",
                    $"EnrollmentId {row.EnrollmentId} bị trùng lặp trong file."));
        }
        return errors;
    }

    private static Task<List<AssessmentImportRow>> ParseAssessmentRowsAsync(Stream fileStream, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var workbook = new XLWorkbook(fileStream);
        var ws = workbook.Worksheet(1);
        var rows = new List<AssessmentImportRow>();
        int lastRow = ws.LastRowUsed()?.RowNumber() ?? AsmDataStartRow - 1;

        for (int r = AsmDataStartRow; r <= lastRow; r++)
        {
            var accountIdCell = ws.Cell(r, AsmColAccountId).GetString().Trim();
            if (string.IsNullOrEmpty(accountIdCell)) continue;

            if (!int.TryParse(accountIdCell, out var accountId)) continue;
            if (!int.TryParse(ws.Cell(r, AsmColSubjectResultId).GetString().Trim(), out var subjectResultId)) continue;

            var scoreStr = ws.Cell(r, AsmColScore).GetString().Trim();
            if (!decimal.TryParse(scoreStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var score))
                score = -1; // sentinel — caught by validation

            var remark = ws.Cell(r, AsmColRemark).GetString().Trim();
            rows.Add(new AssessmentImportRow(r, accountId, subjectResultId, score, string.IsNullOrEmpty(remark) ? null : remark));
        }
        return Task.FromResult(rows);
    }

    private async Task<List<ImportRowError>> ValidateAssessmentRowsAsync(int assessmentId, List<AssessmentImportRow> rows, CancellationToken ct)
    {
        var errors = new List<ImportRowError>();

        var assessment = await _unitOfWork.AssessmentRepository.GetByIdAsync(assessmentId, ct);
        if (assessment == null)
        {
            errors.Add(new ImportRowError(0, "AssessmentId", $"Assessment {assessmentId} không tồn tại."));
            return errors;
        }

        var classesInCourse = (await _unitOfWork.ClassRepository.GetAllAsync(ct))
            .Where(c => c.CourseId == assessment.CourseId && !c.IsDeleted)
            .Select(c => c.ClassId)
            .ToHashSet();

        var activeEnrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(ct))
            .Where(e => !e.IsDeleted && e.Status == EnrollmentStatus.Active && classesInCourse.Contains(e.ClassId))
            .ToList();

        var validAccountIds = activeEnrollments.Select(e => e.AccountId).ToHashSet();
        var activeEnrollmentIds = activeEnrollments.Select(e => e.EnrollmentId).ToHashSet();

        var lockedEtrAccountIds = (await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(ct))
            .Where(e => activeEnrollmentIds.Contains(e.EnrollmentId) && e.IsLocked && !e.IsDeleted)
            .Select(e => activeEnrollments.First(en => en.EnrollmentId == e.EnrollmentId).AccountId)
            .ToHashSet();

        var seenInFile = new HashSet<int>();
        foreach (var row in rows)
        {
            if (row.Score < 0 || row.Score > 100)
                errors.Add(new ImportRowError(row.RowNumber, "Score",
                    $"Điểm '{row.Score}' không hợp lệ. Phải nằm trong khoảng 0-100."));

            if (!validAccountIds.Contains(row.AccountId))
                errors.Add(new ImportRowError(row.RowNumber, "AccountId",
                    $"AccountId {row.AccountId} không có enrollment active trong hệ thống."));

            if (row.SubjectResultId <= 0)
                errors.Add(new ImportRowError(row.RowNumber, "SubjectResultId",
                    $"SubjectResultId {row.SubjectResultId} không hợp lệ. Hãy dùng template được generate từ server."));

            if (lockedEtrAccountIds.Contains(row.AccountId))
                errors.Add(new ImportRowError(row.RowNumber, "AccountId",
                    $"Học viên này đã bị khóa ETR (ETR Locked), không thể thao tác."));

            if (!seenInFile.Add(row.AccountId))
                errors.Add(new ImportRowError(row.RowNumber, "AccountId",
                    $"AccountId {row.AccountId} bị trùng lặp trong file."));
        }
        return errors;
    }

    private async Task RecalculateAttendanceRatesAsync(
        int sessionId, int subjectId, int classId,
        IEnumerable<int> enrollmentIds, CancellationToken ct)
    {
        var allSessions = (await _unitOfWork.SessionRepository.GetAllAsync(ct))
            .Where(s => s.ClassId == classId && s.SubjectId == subjectId && !s.IsDeleted)
            .ToList();
        int totalSessions = allSessions.Count;
        if (totalSessions == 0) return;

        var allRecords = (await _unitOfWork.AttendanceRecordRepository.GetAllAsync(ct))
            .Where(r => allSessions.Select(s => s.SessionId).Contains(r.SessionId) && !r.IsDeleted)
            .ToList();

        foreach (var enrollmentId in enrollmentIds)
        {
            var etr = (await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(ct))
                .FirstOrDefault(e => e.EnrollmentId == enrollmentId && !e.IsDeleted);
            if (etr == null) continue;

            var sr = (await _unitOfWork.SubjectResultRepository.GetAllAsync(ct))
                .FirstOrDefault(s => s.EtrId == etr.ETRCourseRecordId && s.SubjectId == subjectId && !s.IsDeleted);
            if (sr == null) continue;

            int present = allRecords.Count(r =>
                r.EnrollmentId == enrollmentId &&
                r.Status == AttendanceStatus.Present);
            sr.AttendanceRate = totalSessions > 0
                ? Math.Round((decimal)present / totalSessions * 100, 2)
                : 0;
            sr.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.SubjectResultRepository.Update(sr);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  ACCOUNTS (bulk user creation)
    // ════════════════════════════════════════════════════════════════════════

    public async Task<byte[]> GenerateAccountImportTemplateAsync(CancellationToken ct = default)
    {
        var roles = (await _unitOfWork.RoleRepository.GetAllAsync(ct)).Select(r => r.RoleName).OrderBy(n => n).ToList();
        var departments = (await _unitOfWork.DepartmentRepository.GetAllAsync(ct)).Select(d => d.DepartmentName).OrderBy(n => n).ToList();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Tài khoản");

        // ── Row 1: title ──────────────────────────────────────────────────
        ws.Cell(1, 1).Value = "TẠO HÀNG LOẠT TÀI KHOẢN NGƯỜI DÙNG";
        ws.Range(1, 1, 1, AccColDepartmentName).Merge().Style
            .Font.SetBold(true)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        // ── Row 2: column headers ─────────────────────────────────────────
        ws.Cell(2, AccColUsername).Value = "Username (email)*";
        ws.Cell(2, AccColPassword).Value = "Mật khẩu*";
        ws.Cell(2, AccColRoleName).Value = "Vai trò (Role)*";
        ws.Cell(2, AccColDepartmentName).Value = "Phòng ban (Department)*";
        ws.Row(2).Style.Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.LightSteelBlue);

        ws.Columns().AdjustToContents();

        // Dropdown validation for Role/Department columns over a generous row range so users can
        // keep adding rows below the pre-filled ones.
        const int maxTemplateRows = 500;
        var roleRange = ws.Range(AccDataStartRow, AccColRoleName, AccDataStartRow + maxTemplateRows, AccColRoleName);
        roleRange.CreateDataValidation().List($"\"{string.Join(",", roles)}\"", true);
        var deptRange = ws.Range(AccDataStartRow, AccColDepartmentName, AccDataStartRow + maxTemplateRows, AccColDepartmentName);
        deptRange.CreateDataValidation().List($"\"{string.Join(",", departments)}\"", true);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<ImportValidationResult> ValidateAccountImportAsync(Stream fileStream, bool isCallerAdmin, CancellationToken ct = default)
    {
        var rows = await ParseAccountRowsAsync(fileStream, ct);
        var errors = await ValidateAccountRowsAsync(rows, isCallerAdmin, ct);
        return new ImportValidationResult(
            TotalRows: rows.Count,
            ValidRows: rows.Count - errors.Select(e => e.Row).Distinct().Count(),
            ErrorRows: errors.Select(e => e.Row).Distinct().Count(),
            CanCommit: errors.Count == 0,
            Errors: errors);
    }

    public async Task<ImportCommitResult> CommitAccountImportAsync(
        Stream fileStream, int createdByAccountId, bool isCallerAdmin, CancellationToken ct = default)
    {
        var rows = await ParseAccountRowsAsync(fileStream, ct);
        var errors = await ValidateAccountRowsAsync(rows, isCallerAdmin, ct);
        if (errors.Count > 0)
            return new ImportCommitResult(Imported: 0, Skipped: rows.Count, Errors: errors);

        return await _unitOfWork.ExecuteInStrategyAsync(async (innerCt) =>
        {
            await _unitOfWork.BeginTransactionAsync(innerCt);
            try
            {
                var roles = (await _unitOfWork.RoleRepository.GetAllAsync(innerCt))
                    .ToDictionary(r => r.RoleName, r => r.RoleId, StringComparer.OrdinalIgnoreCase);
                var departments = (await _unitOfWork.DepartmentRepository.GetAllAsync(innerCt))
                    .ToDictionary(d => d.DepartmentName, d => d.DepartmentId, StringComparer.OrdinalIgnoreCase);

                var imported = 0;
                foreach (var row in rows)
                {
                    var account = new Account
                    {
                        Username = row.Username,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(row.Password),
                        RoleId = roles[row.RoleName],
                        DepartmentId = departments[row.DepartmentName],
                        Status = AccountStatus.Active,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByAccountId = createdByAccountId
                    };

                    await _unitOfWork.AccountRepository.AddAsync(account, innerCt);
                    await _unitOfWork.SaveAsync(innerCt);

                    await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                    {
                        AccountId = createdByAccountId,
                        ActionType = AuditActionType.INSERT.ToString(),
                        EntityName = nameof(Account),
                        RecordId = account.AccountId,
                        NewValue = account.Username,
                        Description = $"Account #{account.AccountId} ({account.Username}) created via bulk Excel import (row {row.RowNumber})"
                    }, innerCt);

                    imported++;
                }

                await _unitOfWork.SaveAsync(innerCt);
                await _unitOfWork.CommitTransactionAsync(innerCt);

                return new ImportCommitResult(Imported: imported, Skipped: 0, Errors: []);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(innerCt);
                throw;
            }
        }, ct);
    }

    private static Task<List<AccountImportRow>> ParseAccountRowsAsync(Stream fileStream, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var workbook = new XLWorkbook(fileStream);
        var ws = workbook.Worksheet(1);
        var rows = new List<AccountImportRow>();
        int lastRow = ws.LastRowUsed()?.RowNumber() ?? AccDataStartRow - 1;

        for (int r = AccDataStartRow; r <= lastRow; r++)
        {
            var username = ws.Cell(r, AccColUsername).GetString().Trim();
            if (string.IsNullOrEmpty(username)) continue;

            var password = ws.Cell(r, AccColPassword).GetString().Trim();
            var roleName = ws.Cell(r, AccColRoleName).GetString().Trim();
            var departmentName = ws.Cell(r, AccColDepartmentName).GetString().Trim();
            rows.Add(new AccountImportRow(r, username, password, roleName, departmentName));
        }
        return Task.FromResult(rows);
    }

    private async Task<List<ImportRowError>> ValidateAccountRowsAsync(List<AccountImportRow> rows, bool isCallerAdmin, CancellationToken ct)
    {
        var errors = new List<ImportRowError>();

        var roles = (await _unitOfWork.RoleRepository.GetAllAsync(ct))
            .ToDictionary(r => r.RoleName, r => r.RoleId, StringComparer.OrdinalIgnoreCase);
        var departments = (await _unitOfWork.DepartmentRepository.GetAllAsync(ct))
            .Select(d => d.DepartmentName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingUsernames = (await _unitOfWork.AccountRepository.GetAllAsync(ct))
            .Select(a => a.Username)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(row.Username) || row.Username.Length > 255)
                errors.Add(new ImportRowError(row.RowNumber, "Username", $"Username '{row.Username}' phải là email hợp lệ, tối đa 255 ký tự."));

            if (string.IsNullOrWhiteSpace(row.Password))
                errors.Add(new ImportRowError(row.RowNumber, "Password", "Mật khẩu không được để trống."));

            if (!roles.ContainsKey(row.RoleName))
                errors.Add(new ImportRowError(row.RowNumber, "RoleName", $"Vai trò '{row.RoleName}' không tồn tại."));
            else if (!isCallerAdmin && !string.Equals(row.RoleName, "Student", StringComparison.OrdinalIgnoreCase))
                errors.Add(new ImportRowError(row.RowNumber, "RoleName", "Academic staff chỉ được tạo tài khoản Student."));

            if (!departments.Contains(row.DepartmentName))
                errors.Add(new ImportRowError(row.RowNumber, "DepartmentName", $"Phòng ban '{row.DepartmentName}' không tồn tại."));

            if (existingUsernames.Contains(row.Username))
                errors.Add(new ImportRowError(row.RowNumber, "Username", $"Username '{row.Username}' đã tồn tại trong hệ thống."));

            if (!seenInFile.Add(row.Username))
                errors.Add(new ImportRowError(row.RowNumber, "Username", $"Username '{row.Username}' bị trùng lặp trong file."));
        }

        return errors;
    }
}
