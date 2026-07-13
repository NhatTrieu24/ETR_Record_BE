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

    public async Task<AttendanceRecordResponse> RecordAttendanceAsync(CreateAttendanceRecordRequest request, int recordedByUserId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var session = await _unitOfWork.SessionRepository.GetByIdAsync(request.SessionId, ct);
                if (session == null || session.IsConfirmed)
                    throw new InvalidOperationException("Session not found or already confirmed.");

                var record = new AttendanceRecord
                {
                    SessionId = request.SessionId,
                    LearnerId = request.LearnerId,
                    EnrollmentId = request.EnrollmentId,
                    Status = request.Status,
                    Remarks = request.Remarks,
                    RecordedBy = recordedByUserId,
                    RecordedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = recordedByUserId
                };

                await _unitOfWork.AttendanceRecordRepository.AddAsync(record, ct);
                await _unitOfWork.SaveAsync(ct);

                // Auto-calculate AttendanceRate in SubjectResult
                var sr = (await _unitOfWork.SubjectResultRepository.GetAllAsync(ct))
                    .FirstOrDefault(s => s.EnrollmentId == request.EnrollmentId && s.SubjectId == session.SubjectId);
                
                if (sr != null)
                {
                    var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(request.EnrollmentId, ct);
                    if (enrollment != null)
                    {
                        var allRecords = (await _unitOfWork.AttendanceRecordRepository.GetAllAsync(ct))
                            .Where(r => r.EnrollmentId == request.EnrollmentId && r.Status == "Present").ToList();
                        var allSessions = (await _unitOfWork.SessionRepository.GetAllAsync(ct))
                            .Where(s => s.SubjectId == session.SubjectId && s.ClassId == enrollment.ClassId).ToList();

                        if (allSessions.Count > 0)
                        {
                            sr.AttendanceRate = (decimal)allRecords.Count / allSessions.Count * 100;
                            _unitOfWork.SubjectResultRepository.Update(sr);
                        }
                    }
                }

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return new AttendanceRecordResponse(record.AttendanceRecordId, record.SessionId, record.LearnerId, record.EnrollmentId, record.Status, record.Remarks, record.RecordedBy, record.RecordedAt);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    public async Task<AttendanceSessionResponse> ConfirmSessionAsync(int sessionId, int confirmedByUserId, CancellationToken cancellationToken = default)
    {
        var session = await _unitOfWork.SessionRepository.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException("Session not found.");

        session.IsConfirmed = true;
        session.ConfirmedBy = confirmedByUserId;
        session.ConfirmedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        session.UpdatedBy = confirmedByUserId;

        _unitOfWork.SessionRepository.Update(session);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new AttendanceSessionResponse(session.SessionId, session.ClassId, session.SubjectId, session.SessionTitle, session.SessionDate, session.Location, session.IsConfirmed, session.ConfirmedBy, session.ConfirmedAt);
    }
}
