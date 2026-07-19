using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IAttendanceService
{
    Task<IEnumerable<AttendanceRecordResponse>> GetAllAttendanceRecordsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<AttendanceRecordResponse>> GetAttendanceByClassStudentAsync(int classStudentId, int accountId, CancellationToken cancellationToken = default);
    Task<AttendanceRecordResponse> RecordAttendanceAsync(CreateAttendanceRecordRequest request, int recordedByAccountId, CancellationToken cancellationToken = default);
    Task<AttendanceSessionResponse> ConfirmSessionAsync(int sessionId, int confirmedByAccountId, CancellationToken cancellationToken = default);
}
