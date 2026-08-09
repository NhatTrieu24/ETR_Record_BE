using ETR.Application.Compliance;
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

    public async Task<IEnumerable<AttendanceRecordResponse>> GetAllAttendanceRecordsAsync(CancellationToken cancellationToken = default)
    {
        var records = await _unitOfWork.AttendanceRecordRepository.GetAllAsync(cancellationToken);
        return records.Select(r => new AttendanceRecordResponse(
            r.AttendanceRecordId, r.SessionId, r.EnrollmentId, r.Status, r.Remarks, r.RecordedByAccountId, r.RecordedAt));
    }

    public async Task<IEnumerable<AttendanceRecordResponse>> GetAttendanceByEnrollmentAsync(int enrollmentId, int accountId, string? roleName, CancellationToken cancellationToken = default)
    {
        var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(enrollmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Enrollment not found.");

        // Zero-Trust: Students may only view their own attendance records.
        if (roleName == "Student" && enrollment.AccountId != accountId)
        {
            throw new ForbiddenAccessException("You are not authorized to view another student's attendance records.");
        }

        var records = (await _unitOfWork.AttendanceRecordRepository.GetAllAsync(cancellationToken))
            .Where(r => r.EnrollmentId == enrollmentId);

        return records.Select(r => new AttendanceRecordResponse(
            r.AttendanceRecordId, r.SessionId, r.EnrollmentId, r.Status, r.Remarks, r.RecordedByAccountId, r.RecordedAt));
    }

    public async Task<AttendanceRecordResponse> RecordAttendanceAsync(CreateAttendanceRecordRequest request, int recordedByAccountId, string? recordedByRoleName, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var session = await _unitOfWork.SessionRepository.GetByIdAsync(request.SessionId, ct);
                if (session == null || session.IsConfirmed)
                    throw new BusinessRuleViolationException("Session not found or already confirmed.");

                // "Sân nhà ai nấy đá" — Instructor can only record attendance for a class they are
                // actually assigned to (see ClassOwnershipValidator).
                var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(session.ClassId, ct);
                ClassOwnershipValidator.EnsureInstructorOwnsClass(recordedByRoleName, recordedByAccountId, trainingClass?.InstructorAccountId);

                var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(request.EnrollmentId, ct);
                if (enrollment == null || enrollment.ClassId != session.ClassId)
                    throw new BusinessRuleViolationException("Student is not enrolled in this class.");

                var record = new AttendanceRecord
                {
                    SessionId = request.SessionId,
                    EnrollmentId = request.EnrollmentId,
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
                    .FirstOrDefault(etr => etr.EnrollmentId == enrollment.EnrollmentId);

                if (etrRecord != null)
                {
                    var sr = (await _unitOfWork.SubjectResultRepository.GetAllAsync(ct))
                        .FirstOrDefault(s => s.EtrId == etrRecord.ETRCourseRecordId && s.SubjectId == session.SubjectId);

                    if (sr != null)
                    {
                        await RecalculateAttendanceRateAsync(sr, request.EnrollmentId, session.SubjectId, session.ClassId, ct);
                    }
                }

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return new AttendanceRecordResponse(record.AttendanceRecordId, record.SessionId, record.EnrollmentId, record.Status, record.Remarks, record.RecordedByAccountId, record.RecordedAt);
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

    public async Task<AttendanceRecordResponse> GetAttendanceRecordByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var r = await _unitOfWork.AttendanceRecordRepository.GetByIdAsync(id, cancellationToken);
        if (r == null) throw new KeyNotFoundException("AttendanceRecord not found.");
        return new AttendanceRecordResponse(
            r.AttendanceRecordId, r.SessionId, r.EnrollmentId, r.Status, r.Remarks, r.RecordedByAccountId, r.RecordedAt);
    }

    public async Task<AttendanceRecordResponse> UpdateAttendanceRecordAsync(int id, UpdateAttendanceRecordRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var record = await _unitOfWork.AttendanceRecordRepository.GetByIdAsync(id, ct);
                if (record == null) throw new KeyNotFoundException("AttendanceRecord not found.");

                var session = await _unitOfWork.SessionRepository.GetByIdAsync(record.SessionId, ct);
                if (session != null && session.IsConfirmed)
                    throw new BusinessRuleViolationException("Cannot modify an attendance record for a session that has already been confirmed.");

                record.Status = request.Status;
                record.Remarks = request.Remarks;
                record.UpdatedAt = DateTime.UtcNow;
                record.UpdatedByAccountId = updatedByAccountId;

                _unitOfWork.AttendanceRecordRepository.Update(record);
                await _unitOfWork.SaveAsync(ct);

                await RecalculateAttendanceRateForRecordAsync(record, ct);
                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return new AttendanceRecordResponse(
                    record.AttendanceRecordId, record.SessionId, record.EnrollmentId, record.Status, record.Remarks, record.RecordedByAccountId, record.RecordedAt);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    public async Task DeleteAttendanceRecordAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var record = await _unitOfWork.AttendanceRecordRepository.GetByIdAsync(id, ct);
                if (record == null) throw new KeyNotFoundException("AttendanceRecord not found.");

                var session = await _unitOfWork.SessionRepository.GetByIdAsync(record.SessionId, ct);
                if (session != null && session.IsConfirmed)
                    throw new BusinessRuleViolationException("Cannot delete an attendance record for a session that has already been confirmed.");

                record.IsDeleted = true;
                record.DeletedAt = DateTime.UtcNow;
                record.UpdatedAt = DateTime.UtcNow;
                record.UpdatedByAccountId = deletedByAccountId;

                _unitOfWork.AttendanceRecordRepository.Update(record);
                await _unitOfWork.SaveAsync(ct);

                await RecalculateAttendanceRateForRecordAsync(record, ct);
                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);
                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    public async Task<IEnumerable<LowAttendanceStudentResponse>> GetLowAttendanceStudentsAsync(int? classId, CancellationToken cancellationToken = default)
    {
        var subjectResults = (await _unitOfWork.SubjectResultRepository.GetAllAsync(cancellationToken))
            .Where(sr => sr.AttendanceRate.HasValue && sr.AttendanceRate.Value < BusinessRuleEngine.MinimumAttendanceThreshold)
            .ToList();

        if (subjectResults.Count == 0) return Enumerable.Empty<LowAttendanceStudentResponse>();

        var etrs = (await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken)).ToList();
        var enrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken)).ToList();
        var profiles = (await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken)).ToList();
        var classes = (await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken)).ToList();
        var subjects = (await _unitOfWork.SubjectRepository.GetAllAsync(cancellationToken)).ToDictionary(s => s.SubjectId, s => s);

        var result = new List<LowAttendanceStudentResponse>();
        foreach (var sr in subjectResults)
        {
            var etr = etrs.FirstOrDefault(e => e.ETRCourseRecordId == sr.EtrId);
            var enrollment = etr == null ? null : enrollments.FirstOrDefault(e => e.EnrollmentId == etr.EnrollmentId);
            if (enrollment == null) continue;
            if (classId.HasValue && enrollment.ClassId != classId.Value) continue;

            var trainingClass = classes.FirstOrDefault(c => c.ClassId == enrollment.ClassId);
            var profile = profiles.FirstOrDefault(p => p.AccountId == enrollment.AccountId);
            var subject = subjects.GetValueOrDefault(sr.SubjectId);

            result.Add(new LowAttendanceStudentResponse(
                enrollment.AccountId,
                profile?.UserCode ?? "-",
                profile?.FullName ?? "-",
                enrollment.ClassId,
                trainingClass?.ClassCode ?? "-",
                sr.SubjectId,
                subject?.SubjectCode ?? "-",
                sr.AttendanceRate!.Value,
                BusinessRuleEngine.MinimumAttendanceThreshold));
        }

        return result.OrderBy(r => r.AttendanceRate);
    }

    // Ngưỡng đã diễn ra/đã confirm — không tính trên tổng session kế hoạch (mẫu số sai nếu lớp chưa học hết).
    private async Task RecalculateAttendanceRateAsync(SubjectResult sr, int enrollmentId, int subjectId, int classId, CancellationToken ct)
    {
        var confirmedSessions = (await _unitOfWork.SessionRepository.GetAllAsync(ct))
            .Where(s => s.SubjectId == subjectId && s.ClassId == classId && s.IsConfirmed).ToList();

        var confirmedSessionIds = confirmedSessions.Select(s => s.SessionId).ToList();

        var presentRecords = (await _unitOfWork.AttendanceRecordRepository.GetAllAsync(ct))
            .Where(r => r.EnrollmentId == enrollmentId && r.Status == "Present" && confirmedSessionIds.Contains(r.SessionId)).ToList();

        if (confirmedSessions.Count > 0)
        {
            sr.AttendanceRate = (decimal)presentRecords.Count / confirmedSessions.Count * 100;
            _unitOfWork.SubjectResultRepository.Update(sr);
        }
    }

    private async Task RecalculateAttendanceRateForRecordAsync(AttendanceRecord record, CancellationToken ct)
    {
        var session = await _unitOfWork.SessionRepository.GetByIdAsync(record.SessionId, ct);
        if (session == null) return;

        var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(record.EnrollmentId, ct);
        if (enrollment == null) return;

        var etrRecord = (await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(ct))
            .FirstOrDefault(etr => etr.EnrollmentId == enrollment.EnrollmentId);
        if (etrRecord == null) return;

        var sr = (await _unitOfWork.SubjectResultRepository.GetAllAsync(ct))
            .FirstOrDefault(s => s.EtrId == etrRecord.ETRCourseRecordId && s.SubjectId == session.SubjectId);
        if (sr == null) return;

        await RecalculateAttendanceRateAsync(sr, record.EnrollmentId, session.SubjectId, session.ClassId, ct);
    }
}
