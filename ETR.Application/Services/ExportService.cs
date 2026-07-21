using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO.Compression;

namespace ETR.Application.Services;

public class ExportService : IExportService
{
    private readonly IUnitOfWork _unitOfWork;

    public ExportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ExportJobResponse> ExportTrainingPackageAsync(int etrCourseRecordId, int requestedByAccountId, string webRootPath, CancellationToken cancellationToken = default)
    {
        var etr = await _unitOfWork.ETRCourseRecordRepository.GetWithSubjectResultsAsync(etrCourseRecordId, cancellationToken)
            ?? throw new KeyNotFoundException("ETRCourseRecord not found.");

        if (etr.Status != "Completed")
        {
            throw new InvalidOperationException("Cannot export Training Package. ETR is not in Completed status.");
        }

        var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(etr.EnrollmentId, cancellationToken)
            ?? throw new InvalidOperationException("Enrollment not found.");
        var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(enrollment.ClassId, cancellationToken)
            ?? throw new InvalidOperationException("Class not found.");
        var course = await _unitOfWork.CourseRepository.GetByIdAsync(trainingClass.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");
        var profiles = await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken);
        var studentProfile = profiles.FirstOrDefault(p => p.AccountId == enrollment.AccountId);
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

        var pdfBytes = BuildPdf(etr, enrollment, trainingClass, course, studentProfile, subjects, evidenceFiles, approvalHistory);

        var exportDir = Path.Combine(webRootPath, "uploads", "exports");
        Directory.CreateDirectory(exportDir);

        var baseName = $"training-package_etr{etrCourseRecordId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        var zipFileName = $"{baseName}.zip";
        var zipPath = Path.Combine(exportDir, zipFileName);

        using (var zipStream = new FileStream(zipPath, FileMode.Create))
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
        {
            var pdfEntry = archive.CreateEntry($"{baseName}.pdf", CompressionLevel.Optimal);
            using (var entryStream = pdfEntry.Open())
            {
                await entryStream.WriteAsync(pdfBytes, cancellationToken);
            }
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

    private static byte[] BuildPdf(
        ETRCourseRecord etr,
        CourseEnrollment enrollment,
        Class trainingClass,
        Course course,
        UserProfile? studentProfile,
        Dictionary<int, Subject> subjects,
        List<EvidenceFile> evidenceFiles,
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
                    header.Item().Text("Electronic Training Record — Training Package").FontSize(18).Bold();
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

                    column.Item().Text("Evidence Files").FontSize(13).Bold();
                    if (evidenceFiles.Count == 0)
                    {
                        column.Item().Text("(none)");
                    }
                    else
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); c.RelativeColumn(2); });
                            table.Header(h =>
                            {
                                HeaderCell(h, "File Name");
                                HeaderCell(h, "Verification Status");
                                HeaderCell(h, "Verified At");
                            });

                            foreach (var ef in evidenceFiles)
                            {
                                table.Cell().Padding(3).Text(ef.FileName);
                                table.Cell().Padding(3).Text(ef.VerificationStatus);
                                table.Cell().Padding(3).Text(ef.VerifiedAt?.ToString("u") ?? "-");
                            }
                        });
                    }

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
