using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;

namespace ETR.Application.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _unitOfWork;

    public AttendanceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<AttendanceRecordResponse>> GetAttendanceByClassStudentAsync(int classStudentId, int accountId, CancellationToken cancellationToken = default)
    {
        var classStudent = await _unitOfWork.ClassStudentRepository.GetByIdAsync(classStudentId, cancellationToken)
            ?? throw new KeyNotFoundException("ClassStudent not found.");

        // Zero-Trust validation could be enforced here by checking if the requesting accountId is the student's
        var records = (await _unitOfWork.AttendanceRecordRepository.GetAllAsync(cancellationToken))
            .Where(r => r.ClassStudentId == classStudentId);

        return records.Select(r => new AttendanceRecordResponse(
            r.AttendanceRecordId, r.SessionId, r.ClassStudentId, r.Status, r.Remarks, r.RecordedByAccountId, r.RecordedAt));
    }

    public async Task<AttendanceRecordResponse> RecordAttendanceAsync(CreateAttendanceRecordRequest request, int recordedByAccountId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var session = await _unitOfWork.SessionRepository.GetByIdAsync(request.SessionId, ct);
                if (session == null || session.IsConfirmed)
                    throw new InvalidOperationException("Session not found or already confirmed.");

                var classStudent = await _unitOfWork.ClassStudentRepository.GetByIdAsync(request.ClassStudentId, ct);
                if (classStudent == null || classStudent.ClassId != session.ClassId)
                    throw new InvalidOperationException("Student is not assigned to this class.");

                var record = new AttendanceRecord
                {
                    SessionId = request.SessionId,
                    ClassStudentId = request.ClassStudentId,
                    Status = request.Status,
                    Remarks = request.Remarks,
                    RecordedByAccountId = recordedByAccountId,
                    RecordedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByAccountId = recordedByAccountId
                };

                await _unitOfWork.AttendanceRecordRepository.AddAsync(record, ct);
                await _unitOfWork.SaveAsync(ct);

                // Auto-calculate AttendanceRate in SubjectResult
                var etrRecord = (await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(ct))
                    .FirstOrDefault(etr => etr.EnrollmentId == classStudent.CourseEnrollmentId);

                if (etrRecord != null)
                {
                    var sr = (await _unitOfWork.SubjectResultRepository.GetAllAsync(ct))
                        .FirstOrDefault(s => s.EtrId == etrRecord.ETRCourseRecordId && s.SubjectId == session.SubjectId);
                    
                    if (sr != null)
                    {
                        var allRecords = (await _unitOfWork.AttendanceRecordRepository.GetAllAsync(ct))
                            .Where(r => r.ClassStudentId == request.ClassStudentId && r.Status == "Present").ToList();
                        
                        var allSessions = (await _unitOfWork.SessionRepository.GetAllAsync(ct))
                            .Where(s => s.SubjectId == session.SubjectId && s.ClassId == session.ClassId).ToList();

                        if (allSessions.Count > 0)
                        {
                            sr.AttendanceRate = (decimal)allRecords.Count / allSessions.Count * 100;
                            _unitOfWork.SubjectResultRepository.Update(sr);
                        }
                    }
                }

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return new AttendanceRecordResponse(record.AttendanceRecordId, record.SessionId, record.ClassStudentId, record.Status, record.Remarks, record.RecordedByAccountId, record.RecordedAt);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    public async Task<AttendanceSessionResponse> ConfirmSessionAsync(int sessionId, int confirmedByAccountId, CancellationToken cancellationToken = default)
    {
        var session = await _unitOfWork.SessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException("Session not found.");

        session.IsConfirmed = true;
        session.ConfirmedByAccountId = confirmedByAccountId;
        session.ConfirmedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        session.UpdatedByAccountId = confirmedByAccountId;

        _unitOfWork.SessionRepository.Update(session);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new AttendanceSessionResponse(session.SessionId, session.ClassId, session.SubjectId, session.SessionTitle, session.SessionDate, session.Location, session.IsConfirmed, session.ConfirmedByAccountId, session.ConfirmedAt);
    }
}
