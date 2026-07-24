using ClosedXML.Excel;
using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO.Compression;

namespace ETR.Application.Services;

public class ExportService : IExportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExportService> _logger;

    public ExportService(IUnitOfWork unitOfWork, ILogger<ExportService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ExportJobResponse> ExportTrainingPackageAsync(int etrCourseRecordId, int requestedByAccountId, string webRootPath, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetWithSubjectResultsAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException("ETRCourseRecord not found.");

        if (etr.Status != "Completed")
        {
            throw new BusinessRuleViolationException("Cannot export Training Package. ETR is not in Completed status.");
        }

        var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(etr.EnrollmentId, cancellationToken)
            ?? throw new BusinessRuleViolationException("Enrollment not found.");
        var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(enrollment.ClassId, cancellationToken)
            ?? throw new BusinessRuleViolationException("Class not found.");
        var course = await _unitOfWork.CourseRepository.GetByIdAsync(trainingClass.CourseId, cancellationToken)
            ?? throw new BusinessRuleViolationException("Course not found.");
        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        var studentProfile = profiles.FirstOrDefault(p => p.AccountId == enrollment.AccountId)
            ?? throw new BusinessRuleViolationException("Student profile not found for this enrollment.");
        var subjects = (await _unitOfWork.SubjectRepository.GetAllAsync(cancellationToken)).ToDictionary(s => s.SubjectId, s => s);

        var evidenceFiles = (await _unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken))
            .Where(e => etr.SubjectResults.Select(sr => sr.SubjectResultId).Contains(e.SubjectResultId))
            .ToList();

        var approvalRequest = (await _unitOfWork.ApprovalRequestRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(a => a.ETRCourseRecordId == etrCourseRecordId);
        var approvalHistory = approvalRequest == null
            ? new List<ApprovalHistory>()
            : (await _unitOfWork.ApprovalHistoryRepository.GetAllAsync(cancellationToken))
                .Where(h => h.ApprovalRequestId == approvalRequest.ApprovalRequestId)
                .OrderBy(h => h.ActionAt)
                .ToList();

        byte[] summaryPdfBytes;
        byte[] auditHistoryPdfBytes;
        try
        {
            summaryPdfBytes = BuildEtrSummaryPdf(etr, enrollment, trainingClass, course, studentProfile, subjects);
            auditHistoryPdfBytes = BuildAuditHistoryPdf(etr, trainingClass, approvalHistory);
        }
        catch (Exception ex) when (ex is QuestPDF.Drawing.Exceptions.DocumentDrawingException
            or QuestPDF.Drawing.Exceptions.DocumentComposeException
            or QuestPDF.Drawing.Exceptions.DocumentLayoutException)
        {
            // Overlong user-authored strings (approval comments) can overflow the fixed page layout —
            // translate to a domain error instead of a bare 500.
            throw new BusinessRuleViolationException("Could not generate the export PDF because one or more narrative fields are too long. Please shorten comments and retry.");
        }

        byte[] summaryExcelBytes;
        try
        {
            summaryExcelBytes = BuildEtrSummaryExcel(etr, enrollment, trainingClass, course, studentProfile, subjects);
        }
        catch (Exception ex)
        {
            // ClosedXML content here is fully our own bounded/formatted data (IDs, codes, dates) —
            // failure would indicate a genuine bug, not bad input. Catch broadly since ClosedXML's
            // exception surface isn't as well-documented as QuestPDF's, but do NOT echo ex.Message
            // (an unknown/unexpected exception's message is not vetted as client-safe) — log the
            // real detail server-side and return a generic, safe message instead.
            _logger.LogError(ex, "Failed to generate ETR Summary spreadsheet for ETR {EtrCourseRecordId}", etrCourseRecordId);
            throw new BusinessRuleViolationException("Could not generate the export spreadsheet. Please retry or contact an administrator.");
        }

        var exportDir = Path.Combine(webRootPath, "uploads", "exports");
        Directory.CreateDirectory(exportDir);

        var zipFileName = BuildTrainingPackageZipFileName(studentProfile.UserCode, trainingClass.ClassCode);
        var zipPath = Path.Combine(exportDir, zipFileName);

        var attachedEvidenceCount = 0;
        try
        {
            using (var zipStream = new FileStream(zipPath, FileMode.Create))
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                using (var summaryEntryStream = archive.CreateEntry("ETR_Summary.pdf", CompressionLevel.Optimal).Open())
                {
                    await summaryEntryStream.WriteAsync(summaryPdfBytes, cancellationToken);
                }

                using (var auditEntryStream = archive.CreateEntry("Audit_History.pdf", CompressionLevel.Optimal).Open())
                {
                    await auditEntryStream.WriteAsync(auditHistoryPdfBytes, cancellationToken);
                }

                using (var summaryExcelEntryStream = archive.CreateEntry("ETR_Summary.xlsx", CompressionLevel.Optimal).Open())
                {
                    await summaryExcelEntryStream.WriteAsync(summaryExcelBytes, cancellationToken);
                }

                foreach (var ef in evidenceFiles)
                {
                    var physicalPath = Path.Combine(webRootPath, ef.FilePath);
                    if (!File.Exists(physicalPath))
                    {
                        _logger.LogWarning(
                            "Evidence file {EvidenceFileId} ({FileName}) for ETR {EtrCourseRecordId} is missing on disk at {PhysicalPath} — skipped from Training Package export.",
                            ef.EvidenceFileId, ef.FileName, etrCourseRecordId, physicalPath);
                        continue;
                    }

                    var entryName = $"Evidence/{ef.EvidenceFileId}_{SanitizeForFileName(ef.FileName)}";
                    using var evidenceEntryStream = archive.CreateEntry(entryName, CompressionLevel.Optimal).Open();
                    using var fileStream = File.OpenRead(physicalPath);
                    await fileStream.CopyToAsync(evidenceEntryStream, cancellationToken);
                    attachedEvidenceCount++;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
            throw new BusinessRuleViolationException("Could not write the export archive to disk. Please retry or contact an administrator.");
        }

        if (attachedEvidenceCount < evidenceFiles.Count)
        {
            _logger.LogWarning(
                "Training Package export for ETR {EtrCourseRecordId} attached {Attached}/{Total} evidence files — see prior warnings for which were missing.",
                etrCourseRecordId, attachedEvidenceCount, evidenceFiles.Count);
        }

        var job = new ExportJob
        {
            RequestedByAccountId = requestedByAccountId,
            ExportType = "TrainingPackage",
            ETRCourseRecordId = etrCourseRecordId,
            FileName = zipFileName,
            FilePath = Path.Combine("uploads", "exports", zipFileName).Replace("\\", "/"),
            Status = "Completed",
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

    // [Mã_học_viên]_[Mã_lớp]_Training_Package.zip — sanitized against invalid filename
    // characters since UserCode/ClassCode have no format constraint beyond MaxLength.
    public static string BuildTrainingPackageZipFileName(string studentCode, string classCode)
    {
        if (string.IsNullOrWhiteSpace(studentCode))
            throw new ArgumentException("Student code is required.", nameof(studentCode));
        if (string.IsNullOrWhiteSpace(classCode))
            throw new ArgumentException("Class code is required.", nameof(classCode));

        return $"{SanitizeForFileName(studentCode)}_{SanitizeForFileName(classCode)}_Training_Package.zip";
    }

    // Hardcoded rather than Path.GetInvalidFileNameChars(): that API reflects the SERVER's OS
    // (e.g. only '\0' and '/' on Linux/macOS), but this file is downloaded and opened by an
    // auditor on an unknown — likely Windows — machine, so it must be Windows-filename-safe
    // regardless of what OS produced it.
    private static readonly char[] UnsafeFileNameChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    private static string SanitizeForFileName(string value)
    {
        var sanitized = new char[value.Length];
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            sanitized[i] = c < 32 || Array.IndexOf(UnsafeFileNameChars, c) >= 0 ? '_' : c;
        }
        return new string(sanitized);
    }

    // Same ETR Summary content as BuildEtrSummaryPdf, in spreadsheet form — auditors often
    // want the Subject Results table as data they can filter/sort rather than a flat PDF.
    public static byte[] BuildEtrSummaryExcel(
        ETRCourseRecord etr,
        CourseEnrollment enrollment,
        Class trainingClass,
        Course course,
        UserProfile? studentProfile,
        Dictionary<int, Subject> subjects)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("ETR Summary");

        var row = 1;
        void AddInfoRow(string label, string value)
        {
            sheet.Cell(row, 1).Value = label;
            sheet.Cell(row, 1).Style.Font.Bold = true;
            sheet.Cell(row, 2).Value = value;
            row++;
        }

        AddInfoRow("ETR Record ID", etr.ETRCourseRecordId.ToString());
        AddInfoRow("Student", studentProfile?.FullName ?? "(unknown)");
        AddInfoRow("Student Code", studentProfile?.UserCode ?? "(unknown)");
        // Class/Course code and name kept in separate cells (unlike the PDF's combined "Code —
        // Name" display string) so the spreadsheet stays filterable/sortable by exact code.
        AddInfoRow("Class Code", trainingClass.ClassCode);
        AddInfoRow("Class Name", trainingClass.ClassName);
        AddInfoRow("Course Code", course.CourseCode);
        AddInfoRow("Course Name", course.CourseName);
        AddInfoRow("Status", etr.Status);
        AddInfoRow("Submitted At", etr.SubmittedAt?.ToString("u") ?? "-");
        AddInfoRow("Verified At", etr.VerifiedAt?.ToString("u") ?? "-");
        AddInfoRow("Completed At", etr.CompletedAt?.ToString("u") ?? "-");

        row++; // blank separator row
        var tableHeaderRow = row;
        string[] headers = ["Code", "Subject", "Status", "Score", "Attendance %"];
        for (var col = 0; col < headers.Length; col++)
        {
            var cell = sheet.Cell(tableHeaderRow, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        row++;

        foreach (var sr in etr.SubjectResults)
        {
            var subject = subjects.GetValueOrDefault(sr.SubjectId);
            sheet.Cell(row, 1).Value = subject?.SubjectCode ?? sr.SubjectId.ToString();
            sheet.Cell(row, 2).Value = subject?.SubjectName ?? "-";
            sheet.Cell(row, 3).Value = sr.Status;
            sheet.Cell(row, 4).Value = sr.Score?.ToString("0.##") ?? "-";
            sheet.Cell(row, 5).Value = sr.AttendanceRate?.ToString("0.##") ?? "-";
            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] BuildEtrSummaryPdf(
        ETRCourseRecord etr,
        CourseEnrollment enrollment,
        Class trainingClass,
        Course course,
        UserProfile? studentProfile,
        Dictionary<int, Subject> subjects)
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
                    header.Item().Text("Electronic Training Record — Summary").FontSize(18).Bold();
                    header.Item().Text($"{course.CourseCode} — {course.CourseName}").FontSize(12);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Text("ETR Summary").FontSize(13).Bold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                        AddRow(table, "ETR Record ID", etr.ETRCourseRecordId.ToString());
                        AddRow(table, "Student", studentProfile?.FullName ?? "(unknown)");
                        AddRow(table, "Student Code", studentProfile?.UserCode ?? "(unknown)");
                        AddRow(table, "Class", $"{trainingClass.ClassCode} — {trainingClass.ClassName}");
                        AddRow(table, "Status", etr.Status);
                        AddRow(table, "Submitted At", etr.SubmittedAt?.ToString("u") ?? "-");
                        AddRow(table, "Verified At", etr.VerifiedAt?.ToString("u") ?? "-");
                        AddRow(table, "Completed At", etr.CompletedAt?.ToString("u") ?? "-");
                    });

                    column.Item().Text("Subject Results").FontSize(13).Bold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        table.Header(h =>
                        {
                            HeaderCell(h, "Code");
                            HeaderCell(h, "Subject");
                            HeaderCell(h, "Status");
                            HeaderCell(h, "Score");
                            HeaderCell(h, "Attendance %");
                        });

                        foreach (var sr in etr.SubjectResults)
                        {
                            var subject = subjects.GetValueOrDefault(sr.SubjectId);
                            table.Cell().Padding(3).Text(subject?.SubjectCode ?? sr.SubjectId.ToString());
                            table.Cell().Padding(3).Text(subject?.SubjectName ?? "-");
                            table.Cell().Padding(3).Text(sr.Status);
                            table.Cell().Padding(3).Text(sr.Score?.ToString("0.##") ?? "-");
                            table.Cell().Padding(3).Text(sr.AttendanceRate?.ToString("0.##") ?? "-");
                        }
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated ").FontSize(8);
                    text.Span(DateTime.UtcNow.ToString("u")).FontSize(8);
                    text.Span(" — Page ").FontSize(8);
                    text.CurrentPageNumber().FontSize(8);
                    text.Span(" / ").FontSize(8);
                    text.TotalPages().FontSize(8);
                });
            });
        }).GeneratePdf();
    }

    private static byte[] BuildAuditHistoryPdf(
        ETRCourseRecord etr,
        Class trainingClass,
        List<ApprovalHistory> approvalHistory)
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
                    header.Item().Text("Audit History").FontSize(18).Bold();
                    header.Item().Text($"ETR #{etr.ETRCourseRecordId} — {trainingClass.ClassCode}").FontSize(12);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Text("Approval Audit Trail").FontSize(13).Bold();
                    if (approvalHistory.Count == 0)
                    {
                        column.Item().Text("(no approval history recorded)");
                    }
                    else
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(3); c.RelativeColumn(2); c.RelativeColumn(3); });
                            table.Header(h =>
                            {
                                HeaderCell(h, "Action");
                                HeaderCell(h, "Transition");
                                HeaderCell(h, "At");
                                HeaderCell(h, "Comment");
                            });

                            foreach (var entry in approvalHistory)
                            {
                                table.Cell().Padding(3).Text(entry.ActionType);
                                table.Cell().Padding(3).Text($"{entry.PreviousStatus ?? "-"} → {entry.NewStatus ?? "-"}");
                                table.Cell().Padding(3).Text(entry.ActionAt.ToString("u"));
                                table.Cell().Padding(3).Text(entry.Comments ?? "-");
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated ").FontSize(8);
                    text.Span(DateTime.UtcNow.ToString("u")).FontSize(8);
                    text.Span(" — Page ").FontSize(8);
                    text.CurrentPageNumber().FontSize(8);
                    text.Span(" / ").FontSize(8);
                    text.TotalPages().FontSize(8);
                });
            });
        }).GeneratePdf();
    }

    private static void AddRow(TableDescriptor table, string label, string value)
    {
        table.Cell().Padding(3).Text(label).Bold();
        table.Cell().Padding(3).Text(value);
    }

    private static void HeaderCell(TableCellDescriptor header, string text)
    {
        header.Cell().Padding(3).Background(Colors.Grey.Lighten3).Text(text).Bold();
    }
}
