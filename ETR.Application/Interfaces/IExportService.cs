using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IExportService
{
    Task<ExportJobResponse> ExportTrainingPackageAsync(int etrCourseRecordId, int requestedByAccountId, string webRootPath, CancellationToken cancellationToken = default);
}
