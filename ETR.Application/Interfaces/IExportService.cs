using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IExportService
{
    Task<ExportJobResponse> ExportTrainingPackageAsync(int etrCourseRecordId, int requestedByAccountId, string webRootPath, CancellationToken cancellationToken = default);

    Task<ExportJobResponse> ExportEtrPdfAsync(int etrCourseRecordId, int requestedByAccountId, string webRootPath, CancellationToken cancellationToken = default);
    Task<ExportJobResponse> ExportDashboardReportAsync(int requestedByAccountId, string webRootPath, CancellationToken cancellationToken = default);
    Task<ExportJobResponse> ExportAttendanceReportAsync(int classId, int requestedByAccountId, string webRootPath, CancellationToken cancellationToken = default);
    Task<ExportJobResponse> ExportAssessmentReportAsync(int classId, int requestedByAccountId, string webRootPath, CancellationToken cancellationToken = default);
    Task<ExportJobResponse> ExportClassSummaryReportAsync(int classId, int requestedByAccountId, string webRootPath, CancellationToken cancellationToken = default);
}
