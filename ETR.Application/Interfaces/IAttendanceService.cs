using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface IAttendanceService
{
    Task<IEnumerable<AttendanceRecordResponse>> GetAllAttendanceRecordsAsync(CancellationToken cancellationToken = default);
    Task<AttendanceRecordResponse> GetAttendanceRecordByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<AttendanceRecordResponse>> GetAttendanceByEnrollmentAsync(int enrollmentId, int accountId, string? roleName, CancellationToken cancellationToken = default);
    Task<AttendanceRecordResponse> RecordAttendanceAsync(CreateAttendanceRecordRequest request, int recordedByAccountId, string? recordedByRoleName, CancellationToken cancellationToken = default);
    Task<AttendanceRecordResponse> UpdateAttendanceRecordAsync(int id, UpdateAttendanceRecordRequest request, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task DeleteAttendanceRecordAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default);
    Task<AttendanceSessionResponse> ConfirmSessionAsync(int sessionId, int confirmedByAccountId, CancellationToken cancellationToken = default);
    Task<IEnumerable<LowAttendanceStudentResponse>> GetLowAttendanceStudentsAsync(int? classId, CancellationToken cancellationToken = default);
}
