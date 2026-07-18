using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IAttendanceService
{
    Task<AttendanceRecordResponse> RecordAttendanceAsync(CreateAttendanceRecordRequest request, int recordedByAccountId, CancellationToken cancellationToken = default);
    Task<AttendanceSessionResponse> ConfirmSessionAsync(int sessionId, int confirmedByAccountId, CancellationToken cancellationToken = default);
}
