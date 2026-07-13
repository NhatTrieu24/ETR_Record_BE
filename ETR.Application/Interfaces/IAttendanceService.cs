using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IAttendanceService
{
    Task<AttendanceRecordResponse> RecordAttendanceAsync(CreateAttendanceRecordRequest request, int recordedByUserId, CancellationToken cancellationToken = default);
    Task<AttendanceSessionResponse> ConfirmSessionAsync(int sessionId, int confirmedByUserId, CancellationToken cancellationToken = default);
}
