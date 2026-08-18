using ClosedXML.Excel;
using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Domain.Entities;
using ETR.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ETR.Application.Services;

// H8/H9/H12: standalone report exports (independent PDF/Dashboard, Attendance/Assessment, and
// Class/Course aggregate Excel) — previously these three export types were either mocked
// (PDF/Dashboard) or missing entirely (Attendance/Assessment/Class summary). All reuse the same
// disk-write + ExportJob bookkeeping as ExportTrainingPackageAsync, factored into WriteExportFileAsync.
public partial class ExportService
{
    public async Task<ExportJobResponse> ExportEtrPdfAsync(int etrCourseRecordId, int requestedByAccountId, string webRootPath, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetWithSubjectResultsAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException("ETRCourseRecord not found.");

        var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(etr.EnrollmentId, cancellationToken)
            ?? throw new BusinessRuleViolationException("Enrollment not found.");
        var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(enrollment.ClassId, cancellationToken)
            ?? throw new BusinessRuleViolationException("Class not found.");
        var course = await _unitOfWork.CourseRepository.GetByIdAsync(trainingClass.CourseId, cancellationToken)
            ?? throw new BusinessRuleViolationException("Course not found.");
        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        var studentProfile = profiles.FirstOrDefault(p => p.AccountId == enrollment.AccountId);
        var subjects = (await _unitOfWork.SubjectRepository.GetAllAsync(cancellationToken)).ToDictionary(s => s.SubjectId, s => s);

        byte[] pdfBytes;
        try
        {
            pdfBytes = BuildEtrSummaryPdf(etr, enrollment, trainingClass, course, studentProfile, subjects);
        }
        catch (Exception ex) when (ex is QuestPDF.Drawing.Exceptions.DocumentDrawingException
            or QuestPDF.Drawing.Exceptions.DocumentComposeException
            or QuestPDF.Drawing.Exceptions.DocumentLayoutException)
        {
            throw new BusinessRuleViolationException("Could not generate the export PDF because one or more narrative fields are too long. Please shorten comments and retry.");
        }

        var fileName = $"ETR_{etrCourseRecordId}_Summary_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        return await WriteExportFileAsync("PDF", fileName, pdfBytes, requestedByAccountId, webRootPath, etrCourseRecordId, cancellationToken);
    }

    public async Task<ExportJobResponse> ExportDashboardReportAsync(int requestedByAccountId, string webRootPath, CancellationToken cancellationToken = default)
    {
        var classes = await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken);
        var kpis = await DashboardKpiCalculator.ComputeAsync(_unitOfWork, cancellationToken);

        var pdfBytes = BuildDashboardPdf(classes.Count(), kpis);
        var fileName = $"Dashboard_Summary_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        return await WriteExportFileAsync("Dashboard", fileName, pdfBytes, requestedByAccountId, webRootPath, null, cancellationToken);
    }

    public async Task<ExportJobResponse> ExportAttendanceReportAsync(int classId, int requestedByAccountId, string webRootPath, CancellationToken cancellationToken = default)
    {
        var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(classId, cancellationToken)
            ?? throw new KeyNotFoundException("Class not found.");

        var enrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken))
            .Where(e => e.ClassId == classId).ToList();
        var enrollmentIds = enrollments.Select(e => e.EnrollmentId).ToHashSet();

        var sessions = (await _unitOfWork.SessionRepository.GetAllAsync(cancellationToken))
            .Where(s => s.ClassId == classId)
            .ToDictionary(s => s.SessionId, s => s);

        var records = (await _unitOfWork.AttendanceRecordRepository.GetAllAsync(cancellationToken))
            .Where(r => enrollmentIds.Contains(r.EnrollmentId))
            .OrderBy(r => sessions.GetValueOrDefault(r.SessionId)?.SessionDate)
            .ToList();

        var profiles = (await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken)).ToList();

        var excelBytes = BuildAttendanceReportExcel(trainingClass, enrollments, profiles, sessions, records);
        var fileName = $"{SanitizeForFileName(trainingClass.ClassCode)}_Attendance_Report_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return await WriteExportFileAsync("AttendanceReport", fileName, excelBytes, requestedByAccountId, webRootPath, null, cancellationToken);
    }

    public async Task<ExportJobResponse> ExportAssessmentReportAsync(int classId, int requestedByAccountId, string webRootPath, CancellationToken cancellationToken = default)
    {
        var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(classId, cancellationToken)
            ?? throw new KeyNotFoundException("Class not found.");

        var enrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken))
            .Where(e => e.ClassId == classId).ToList();
        var enrollmentAccountIds = enrollments.Select(e => e.AccountId).ToHashSet();

        var sessions = (await _unitOfWork.SessionRepository.GetAllAsync(cancellationToken))
            .Where(s => s.ClassId == classId)
            .ToDictionary(s => s.SessionId, s => s);

        var subjects = (await _unitOfWork.SubjectRepository.GetAllAsync(cancellationToken)).ToDictionary(s => s.SubjectId, s => s);

        var results = (await _unitOfWork.AssessmentResultRepository.GetAllAsync(cancellationToken))
            .Where(r => enrollmentAccountIds.Contains(r.AccountId) && (r.SessionId == null || sessions.ContainsKey(r.SessionId.Value)))
            .OrderBy(r => r.RecordedAt)
            .ToList();

        var profiles = (await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken)).ToList();

        var excelBytes = BuildAssessmentReportExcel(trainingClass, results, profiles, sessions, subjects);
        var fileName = $"{SanitizeForFileName(trainingClass.ClassCode)}_Assessment_Report_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return await WriteExportFileAsync("AssessmentReport", fileName, excelBytes, requestedByAccountId, webRootPath, null, cancellationToken);
    }

    public async Task<ExportJobResponse> ExportClassSummaryReportAsync(int classId, int requestedByAccountId, string webRootPath, CancellationToken cancellationToken = default)
    {
        var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(classId, cancellationToken)
            ?? throw new KeyNotFoundException("Class not found.");
        var course = await _unitOfWork.CourseRepository.GetByIdAsync(trainingClass.CourseId, cancellationToken)
            ?? throw new BusinessRuleViolationException("Course not found.");

        var enrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken))
            .Where(e => e.ClassId == classId).ToList();
        var enrollmentIds = enrollments.Select(e => e.EnrollmentId).ToHashSet();

        var etrs = (await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken))
            .Where(e => enrollmentIds.Contains(e.EnrollmentId)).ToList();

        var profiles = (await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken)).ToList();

        var excelBytes = BuildClassSummaryExcel(trainingClass, course, enrollments, etrs, profiles);
        var fileName = $"{SanitizeForFileName(trainingClass.ClassCode)}_Class_Summary_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return await WriteExportFileAsync("ClassSummary", fileName, excelBytes, requestedByAccountId, webRootPath, null, cancellationToken);
    }

    // Shared "write bytes to disk + record ExportJob" tail, mirroring ExportTrainingPackageAsync's
    // own disk-write block — factored here since 5 export types now need the identical bookkeeping.
    private async Task<ExportJobResponse> WriteExportFileAsync(
        string exportType,
        string fileName,
        byte[] content,
        int requestedByAccountId,
        string webRootPath,
        int? etrCourseRecordId,
        CancellationToken cancellationToken)
    {
        var exportDir = Path.Combine(webRootPath, "uploads", "exports");
        Directory.CreateDirectory(exportDir);
        var filePath = Path.Combine(exportDir, fileName);

        try
        {
            await File.WriteAllBytesAsync(filePath, content, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new BusinessRuleViolationException("Could not write the export file to disk. Please retry or contact an administrator.");
        }

        var job = new ExportJob
        {
            RequestedByAccountId = requestedByAccountId,
            ExportType = exportType,
            ETRCourseRecordId = etrCourseRecordId,
            FileName = fileName,
            FilePath = Path.Combine("uploads", "exports", fileName).Replace("\\", "/"),
            Status = ExportJobStatus.Completed,
            RequestedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            DownloadExpiredAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = requestedByAccountId
        };

        await _unitOfWork.ExportJobRepository.AddAsync(job, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new ExportJobResponse(
            job.ExportJobId, job.RequestedByAccountId, job.ExportType, job.FileName!, job.FilePath!,
            job.Status, job.RequestedAt, job.CompletedAt, job.DownloadExpiredAt, job.ETRCourseRecordId);
    }

    private static byte[] BuildDashboardPdf(int totalClasses, DashboardKpis kpis)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(header =>
                {
                    header.Item().Text("Dashboard Summary").FontSize(18).Bold();
                    header.Item().Text(DateTime.UtcNow.ToString("u")).FontSize(10);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                        AddRow(table, "Total Classes", totalClasses.ToString());
                        AddRow(table, "Total ETRs", kpis.TotalEtrs.ToString());
                        AddRow(table, "Completed ETRs", kpis.CompletedCount.ToString());
                        AddRow(table, "Completion Rate", $"{kpis.CompletionRatePercent}%");
                        AddRow(table, "Pending Approval", kpis.PendingApprovalCount.ToString());
                        AddRow(table, "Rejected", kpis.RejectedCount.ToString());
                        AddRow(table, "Returned For Correction", kpis.ReturnedForCorrectionCount.ToString());
                        AddRow(table, "Missing Evidence", kpis.MissingEvidenceCount.ToString());
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated ").FontSize(8);
                    text.Span(DateTime.UtcNow.ToString("u")).FontSize(8);
                });
            });
        }).GeneratePdf();
    }

    private static byte[] BuildAttendanceReportExcel(
        Class trainingClass,
        List<CourseEnrollment> enrollments,
        List<UserProfile> profiles,
        Dictionary<int, Session> sessions,
        List<AttendanceRecord> records)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Attendance");

        sheet.Cell(1, 1).Value = $"Attendance Report — {trainingClass.ClassCode} ({trainingClass.ClassName})";
        sheet.Cell(1, 1).Style.Font.Bold = true;

        var headerRow = 3;
        string[] headers = ["Student Code", "Student Name", "Session", "Session Date", "Status", "Remarks"];
        for (var col = 0; col < headers.Length; col++)
        {
            var cell = sheet.Cell(headerRow, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var row = headerRow + 1;
        foreach (var record in records)
        {
            var enrollment = enrollments.FirstOrDefault(e => e.EnrollmentId == record.EnrollmentId);
            var profile = enrollment == null ? null : profiles.FirstOrDefault(p => p.AccountId == enrollment.AccountId);
            var session = sessions.GetValueOrDefault(record.SessionId);

            sheet.Cell(row, 1).Value = profile?.UserCode ?? "-";
            sheet.Cell(row, 2).Value = profile?.FullName ?? "-";
            sheet.Cell(row, 3).Value = session?.SessionTitle ?? record.SessionId.ToString();
            sheet.Cell(row, 4).Value = session?.SessionDate?.ToString("yyyy-MM-dd") ?? "-";
            sheet.Cell(row, 5).Value = record.Status.ToString();
            sheet.Cell(row, 6).Value = record.Remarks ?? "-";
            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] BuildAssessmentReportExcel(
        Class trainingClass,
        List<AssessmentResult> results,
        List<UserProfile> profiles,
        Dictionary<int, Session> sessions,
        Dictionary<int, Subject> subjects)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Assessment");

        sheet.Cell(1, 1).Value = $"Assessment Report — {trainingClass.ClassCode} ({trainingClass.ClassName})";
        sheet.Cell(1, 1).Style.Font.Bold = true;

        var headerRow = 3;
        string[] headers = ["Student Name", "Subject", "Score", "Status", "Attempt #", "Taken At", "Published"];
        for (var col = 0; col < headers.Length; col++)
        {
            var cell = sheet.Cell(headerRow, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var row = headerRow + 1;
        foreach (var result in results)
        {
            var profile = profiles.FirstOrDefault(p => p.AccountId == result.AccountId);
            var session = result.SessionId.HasValue ? sessions.GetValueOrDefault(result.SessionId.Value) : null;
            var subject = session != null ? subjects.GetValueOrDefault(session.SubjectId) : null;

            sheet.Cell(row, 1).Value = profile?.FullName ?? "-";
            sheet.Cell(row, 2).Value = subject?.SubjectName ?? "-";
            sheet.Cell(row, 3).Value = result.Score.ToString("0.##");
            sheet.Cell(row, 4).Value = result.ResultStatus;
            sheet.Cell(row, 5).Value = result.AttemptNo.ToString();
            sheet.Cell(row, 6).Value = result.TakenAt?.ToString("yyyy-MM-dd") ?? "-";
            sheet.Cell(row, 7).Value = result.IsPublished ? "Yes" : "No";
            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // H12: aggregate summary across every student in a class/course, one row each — the existing
    // BuildEtrSummaryExcel only ever covers a single ETR/student, embedded in that student's own
    // Training Package zip.
    private static byte[] BuildClassSummaryExcel(
        Class trainingClass,
        Course course,
        List<CourseEnrollment> enrollments,
        List<ETRCourseRecord> etrs,
        List<UserProfile> profiles)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Class Summary");

        sheet.Cell(1, 1).Value = $"Class Summary — {trainingClass.ClassCode} ({trainingClass.ClassName})";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(2, 1).Value = $"Course: {course.CourseCode} — {course.CourseName}";

        var headerRow = 4;
        string[] headers = ["Student Code", "Student Name", "ETR Status", "Issued Date", "Expiry Date"];
        for (var col = 0; col < headers.Length; col++)
        {
            var cell = sheet.Cell(headerRow, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var row = headerRow + 1;
        foreach (var enrollment in enrollments)
        {
            var profile = profiles.FirstOrDefault(p => p.AccountId == enrollment.AccountId);
            var etr = etrs.FirstOrDefault(e => e.EnrollmentId == enrollment.EnrollmentId);

            sheet.Cell(row, 1).Value = profile?.UserCode ?? "-";
            sheet.Cell(row, 2).Value = profile?.FullName ?? "-";
            sheet.Cell(row, 3).Value = etr?.Status.ToString() ?? "(no ETR)";
            sheet.Cell(row, 4).Value = etr?.IssuedDate?.ToString("yyyy-MM-dd") ?? "-";
            sheet.Cell(row, 5).Value = etr?.ExpiryDate?.ToString("yyyy-MM-dd") ?? "-";
            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
