using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Domain.Enums;

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
        var sessionIds = records.Select(r => r.SessionId).Distinct().ToList();
        var sessions = (await _unitOfWork.SessionRepository.GetAllAsync(cancellationToken))
            .Where(s => sessionIds.Contains(s.SessionId))
            .ToDictionary(s => s.SessionId, s => s);

        return records.Select(r => MapToResponse(r, sessions.GetValueOrDefault(r.SessionId)));
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
            .Where(r => r.EnrollmentId == enrollmentId).ToList();

        var sessionIds = records.Select(r => r.SessionId).Distinct().ToList();
        var sessions = (await _unitOfWork.SessionRepository.GetAllAsync(cancellationToken))
            .Where(s => sessionIds.Contains(s.SessionId))
            .ToDictionary(s => s.SessionId, s => s);

        return records.Select(r => MapToResponse(r, sessions.GetValueOrDefault(r.SessionId)));
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

                // Validate flight/SIM hours and metadata against session training type and attendance status
                ValidateTrainingRecord(session, request.Status, request.PerformanceGrade,
                    request.FlightHours, request.SimulatorHours, request.DualHours, request.SoloHours,
                    request.PicHours, request.NightHours, request.InstrumentHours, request.CrossCountryHours,
                    request.DayLandings, request.NightLandings, request.AircraftRegistration, request.SimulatorDevice,
                    request.DepartureIcao, request.ArrivalIcao, request.Route);

                // Grace Period (48 hours)
                if (session.SessionDate.HasValue)
                {
                    if (session.SessionDate.Value.Date > DateTime.UtcNow.Date)
                    {
                        throw new BusinessRuleViolationException("Không thể điểm danh trước cho buổi học trong tương lai.");
                    }

                    var sessionExpiryTime = session.SessionDate.Value.Date.AddDays(1).AddHours(BusinessRuleEngine.AttendanceGracePeriodHours);
                    if (DateTime.UtcNow > sessionExpiryTime && string.Equals(recordedByRoleName, "Instructor", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new BusinessRuleViolationException($"Buổi học ngày {session.SessionDate.Value:dd/MM/yyyy} đã vượt quá thời gian cho phép điểm danh bù ({BusinessRuleEngine.AttendanceGracePeriodHours} giờ). Vui lòng liên hệ Academic Staff để xử lý ngoại lệ.");
                    }
                }

                // Instructor ownership check
                var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(session.ClassId, ct);
                if (trainingClass != null && trainingClass.Status != ClassStatus.InProgress)
                {
                    throw new BusinessRuleViolationException($"Không thể điểm danh. Lớp học '{trainingClass.ClassName}' chưa bắt đầu hoặc không còn hoạt động (Trạng thái: {trainingClass.Status}). Chỉ có thể điểm danh khi lớp học đang ở trạng thái 'Đang diễn ra' (InProgress).");
                }
                var isAssigned = trainingClass != null && _unitOfWork.ClassSubjectRepository.GetQueryable()
                    .Any(cs => cs.ClassId == trainingClass.ClassId && cs.SubjectId == session.SubjectId && cs.InstructorAccountId == recordedByAccountId);
                ClassOwnershipValidator.EnsureInstructorOwnsSubject(recordedByRoleName, isAssigned);

                var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(request.EnrollmentId, ct);
                if (enrollment == null || enrollment.ClassId != session.ClassId)
                    throw new BusinessRuleViolationException("Student is not enrolled in this class.");

                var existingRecords = await _unitOfWork.AttendanceRecordRepository.GetAllAsync(ct);
                if (existingRecords.Any(r => r.SessionId == request.SessionId && r.EnrollmentId == request.EnrollmentId))
                {
                    throw new BusinessRuleViolationException("An attendance record for this session and enrollment already exists.");
                }

                var record = new AttendanceRecord
                {
                    SessionId = request.SessionId,
                    EnrollmentId = request.EnrollmentId,
                    Status = request.Status,
                    Remarks = request.Remarks,
                    PerformanceGrade = request.PerformanceGrade,
                    FlightHours = request.FlightHours,
                    SimulatorHours = request.SimulatorHours,
                    DualHours = request.DualHours,
                    SoloHours = request.SoloHours,
                    PicHours = request.PicHours,
                    NightHours = request.NightHours,
                    InstrumentHours = request.InstrumentHours,
                    CrossCountryHours = request.CrossCountryHours,
                    DayLandings = request.DayLandings,
                    NightLandings = request.NightLandings,
                    AircraftRegistration = request.AircraftRegistration,
                    SimulatorDevice = request.SimulatorDevice,
                    DepartureIcao = request.DepartureIcao,
                    ArrivalIcao = request.ArrivalIcao,
                    Route = request.Route,
                    InstructorComments = request.InstructorComments,
                    StudentComments = request.StudentComments,
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

                await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                {
                    AccountId = recordedByAccountId,
                    ActionType = AuditActionType.INSERT.ToString(),
                    EntityName = nameof(AttendanceRecord),
                    RecordId = record.AttendanceRecordId,
                    ETRRecordId = etrRecord?.ETRCourseRecordId,
                    NewValue = request.Status.ToString(),
                    Description = $"Recorded attendance status '{request.Status}' (Type: {session.TrainingType}) for Student Enrollment #{request.EnrollmentId} in Session #{request.SessionId}"
                }, ct);

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return MapToResponse(record, session);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    public async Task<AttendanceSessionResponse> ConfirmSessionAsync(int sessionId, int confirmedByAccountId, string? roleName = null, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var session = await _unitOfWork.SessionRepository.GetByIdAsync(sessionId, ct)
                    ?? throw new KeyNotFoundException("Session not found.");

                // Instructor assignment check (Admin is permitted by ClassOwnershipValidator policy)
                var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(session.ClassId, ct);
                if (trainingClass != null && trainingClass.Status != ClassStatus.InProgress)
                {
                    throw new BusinessRuleViolationException($"Không thể xác nhận buổi học. Lớp học '{trainingClass.ClassName}' chưa bắt đầu hoặc không còn hoạt động (Trạng thái: {trainingClass.Status}). Chỉ có thể xác nhận khi lớp học đang ở trạng thái 'Đang diễn ra' (InProgress).");
                }
                var isAssigned = trainingClass != null && _unitOfWork.ClassSubjectRepository.GetQueryable()
                    .Any(cs => cs.ClassId == trainingClass.ClassId && cs.SubjectId == session.SubjectId && cs.InstructorAccountId == confirmedByAccountId);
                ClassOwnershipValidator.EnsureInstructorOwnsSubject(roleName, isAssigned);

                var enrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(ct))
                    .Where(e => e.ClassId == session.ClassId && !e.IsDeleted).ToList();

                // For Flight and Simulator sessions, verify all enrolled students have attendance records and all records are digitally signed by the instructor
                if (session.TrainingType == TrainingType.Flight || session.TrainingType == TrainingType.Simulator)
                {
                    var sessionRecords = (await _unitOfWork.AttendanceRecordRepository.GetAllAsync(ct))
                        .Where(r => r.SessionId == sessionId && !r.IsDeleted).ToList();

                    var recordedEnrollmentIds = sessionRecords.Select(r => r.EnrollmentId).ToHashSet();
                    var missingEnrollments = enrollments.Where(e => !recordedEnrollmentIds.Contains(e.EnrollmentId)).ToList();
                    if (missingEnrollments.Count > 0)
                    {
                        throw new BusinessRuleViolationException($"Cannot confirm {session.TrainingType} session #{sessionId} because {missingEnrollments.Count} enrolled student(s) in class #{session.ClassId} do not have an attendance record.");
                    }

                    var unsignedRecords = sessionRecords.Where(r => !r.InstructorSignedAt.HasValue).ToList();
                    if (unsignedRecords.Count > 0)
                    {
                        throw new BusinessRuleViolationException($"Cannot confirm {session.TrainingType} session #{sessionId} because {unsignedRecords.Count} attendance record(s) have not been digitally signed by the instructor.");
                    }
                }

                session.IsConfirmed = true;
                session.ConfirmedByAccountId = confirmedByAccountId;
                session.ConfirmedAt = DateTime.UtcNow;
                session.UpdatedAt = DateTime.UtcNow;
                session.UpdatedByAccountId = confirmedByAccountId;

                _unitOfWork.SessionRepository.Update(session);
                await _unitOfWork.SaveAsync(ct);

                var etrs = (await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(ct))
                    .Where(e => enrollments.Select(en => en.EnrollmentId).Contains(e.EnrollmentId)).ToList();
                    
                var subjectResults = (await _unitOfWork.SubjectResultRepository.GetAllAsync(ct))
                    .Where(sr => sr.SubjectId == session.SubjectId && etrs.Select(e => e.ETRCourseRecordId).Contains(sr.EtrId)).ToList();

                foreach (var enrollment in enrollments)
                {
                    var etr = etrs.FirstOrDefault(e => e.EnrollmentId == enrollment.EnrollmentId);
                    if (etr == null) continue;

                    var sr = subjectResults.FirstOrDefault(s => s.EtrId == etr.ETRCourseRecordId);
                    if (sr == null) continue;

                    await RecalculateAttendanceRateAsync(sr, enrollment.EnrollmentId, session.SubjectId, session.ClassId, ct);
                }

                // Tự động hoàn thành lớp (Completed) khi toàn bộ các buổi đào tạo của lớp đã được xác nhận (Confirm)
                if (trainingClass != null && trainingClass.Status == ClassStatus.InProgress && !trainingClass.IsDeleted)
                {
                    var allSessions = (await _unitOfWork.SessionRepository.GetAllAsync(ct))
                        ?.Where(s => s.ClassId == session.ClassId && !s.IsDeleted).ToList()
                        ?? new List<Session>();

                    bool allSessionsConfirmed = allSessions.Count > 0 && allSessions.All(s => s.SessionId == sessionId || s.IsConfirmed);

                    if (allSessionsConfirmed)
                    {
                        var courseSubjects = _unitOfWork.CourseSubjectRepository != null
                            ? (await _unitOfWork.CourseSubjectRepository.GetAllAsync(ct))
                                ?.Where(cs => cs.CourseId == trainingClass.CourseId).ToList()
                            : null;

                        var sessionSubjectIds = allSessions.Select(s => s.SubjectId).Distinct().ToHashSet();
                        bool allSubjectsCovered = courseSubjects == null || courseSubjects.Count == 0 || courseSubjects.All(cs => sessionSubjectIds.Contains(cs.SubjectId));

                        if (allSubjectsCovered)
                        {
                            var oldClassStatus = trainingClass.Status;
                            trainingClass.Status = ClassStatus.Completed;
                            trainingClass.UpdatedAt = DateTime.UtcNow;
                            trainingClass.UpdatedByAccountId = confirmedByAccountId;
                            _unitOfWork.ClassRepository.Update(trainingClass);

                            await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                            {
                                AccountId = confirmedByAccountId,
                                ActionType = AuditActionType.UPDATE.ToString(),
                                EntityName = nameof(Class),
                                RecordId = trainingClass.ClassId,
                                OldValue = $"Status: {oldClassStatus}",
                                NewValue = $"Status: {ClassStatus.Completed}",
                                Description = $"Lớp học #{trainingClass.ClassId} ('{trainingClass.ClassName}') đã hoàn thành tất cả các môn học và buổi đào tạo, tự động chuyển sang trạng thái Completed."
                            }, ct);
                        }
                    }
                }

                await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                {
                    AccountId = confirmedByAccountId,
                    ActionType = AuditActionType.UPDATE.ToString(),
                    EntityName = nameof(Session),
                    RecordId = sessionId,
                    OldValue = "IsConfirmed: False",
                    NewValue = "IsConfirmed: True",
                    Description = $"Session #{sessionId} ('{session.SessionTitle}') confirmed and attendance rates recalculated for class #{session.ClassId}"
                }, ct);

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return new AttendanceSessionResponse(session.SessionId, session.ClassId, session.SubjectId, session.SessionTitle, session.SessionDate, session.Location, session.IsConfirmed, session.ConfirmedByAccountId, session.ConfirmedAt);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    public async Task<AttendanceRecordResponse> GetAttendanceRecordByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var r = await _unitOfWork.AttendanceRecordRepository.GetByIdAsync(id, cancellationToken);
        if (r == null) throw new KeyNotFoundException("AttendanceRecord not found.");
        var session = await _unitOfWork.SessionRepository.GetByIdAsync(r.SessionId, cancellationToken);
        return MapToResponse(r, session);
    }

    public async Task<AttendanceRecordResponse> UpdateAttendanceRecordAsync(int id, UpdateAttendanceRecordRequest request, int updatedByAccountId, string? roleName = null, CancellationToken cancellationToken = default)
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

                if (session != null)
                {
                    // Instructor ownership check
                    var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(session.ClassId, ct);
                    if (trainingClass != null && trainingClass.Status != ClassStatus.InProgress)
                    {
                        throw new BusinessRuleViolationException($"Không thể cập nhật điểm danh. Lớp học '{trainingClass.ClassName}' chưa bắt đầu hoặc không còn hoạt động (Trạng thái: {trainingClass.Status}). Chỉ có thể chỉnh sửa điểm danh khi lớp học đang ở trạng thái 'Đang diễn ra' (InProgress).");
                    }
                    var isAssigned = trainingClass != null && _unitOfWork.ClassSubjectRepository.GetQueryable()
                        .Any(cs => cs.ClassId == trainingClass.ClassId && cs.SubjectId == session.SubjectId && cs.InstructorAccountId == updatedByAccountId);
                    ClassOwnershipValidator.EnsureInstructorOwnsSubject(roleName, isAssigned);

                    // Validate training details
                    ValidateTrainingRecord(session, request.Status, request.PerformanceGrade,
                        request.FlightHours, request.SimulatorHours, request.DualHours, request.SoloHours,
                        request.PicHours, request.NightHours, request.InstrumentHours, request.CrossCountryHours,
                        request.DayLandings, request.NightLandings, request.AircraftRegistration, request.SimulatorDevice,
                        request.DepartureIcao, request.ArrivalIcao, request.Route);
                }

                var oldStatus = record.Status;
                bool signaturesInvalidated = false;
                if (record.InstructorSignedAt.HasValue || record.StudentSignedAt.HasValue)
                {
                    record.InstructorSignedAt = null;
                    record.InstructorSignedByAccountId = null;
                    record.StudentSignedAt = null;
                    record.StudentSignedByAccountId = null;
                    signaturesInvalidated = true;
                }

                record.Status = request.Status;
                record.Remarks = request.Remarks;
                record.PerformanceGrade = request.PerformanceGrade;
                record.FlightHours = request.FlightHours;
                record.SimulatorHours = request.SimulatorHours;
                record.DualHours = request.DualHours;
                record.SoloHours = request.SoloHours;
                record.PicHours = request.PicHours;
                record.NightHours = request.NightHours;
                record.InstrumentHours = request.InstrumentHours;
                record.CrossCountryHours = request.CrossCountryHours;
                record.DayLandings = request.DayLandings;
                record.NightLandings = request.NightLandings;
                record.AircraftRegistration = request.AircraftRegistration;
                record.SimulatorDevice = request.SimulatorDevice;
                record.DepartureIcao = request.DepartureIcao;
                record.ArrivalIcao = request.ArrivalIcao;
                record.Route = request.Route;
                record.InstructorComments = request.InstructorComments;
                record.StudentComments = request.StudentComments;
                record.UpdatedAt = DateTime.UtcNow;
                record.UpdatedByAccountId = updatedByAccountId;

                if (oldStatus != request.Status)
                {
                    await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                    {
                        AccountId = updatedByAccountId,
                        ActionType = AuditActionType.UPDATE.ToString(),
                        EntityName = "AttendanceRecord",
                        RecordId = id,
                        OldValue = oldStatus.ToString(),
                        NewValue = request.Status.ToString(),
                        Description = $"Updated AttendanceRecord status from {oldStatus} to {request.Status}",
                        CreatedAt = DateTime.UtcNow
                    }, ct);
                }

                if (signaturesInvalidated)
                {
                    await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                    {
                        AccountId = updatedByAccountId,
                        ActionType = AuditActionType.UPDATE.ToString(),
                        EntityName = nameof(AttendanceRecord),
                        RecordId = id,
                        Description = $"Digital signatures invalidated due to training record update by account #{updatedByAccountId}",
                        CreatedAt = DateTime.UtcNow
                    }, ct);
                }

                _unitOfWork.AttendanceRecordRepository.Update(record);
                await _unitOfWork.SaveAsync(ct);

                await RecalculateAttendanceRateForRecordAsync(record, ct);
                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return MapToResponse(record, session);
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
            .Where(r => r.EnrollmentId == enrollmentId && r.Status == AttendanceStatus.Present && confirmedSessionIds.Contains(r.SessionId)).ToList();

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

    public async Task<AttendanceRecordResponse> InstructorSignOffAsync(int id, SignAttendanceRecordRequest request, int instructorAccountId, string? roleName, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var record = await _unitOfWork.AttendanceRecordRepository.GetByIdAsync(id, ct)
                    ?? throw new KeyNotFoundException("AttendanceRecord not found.");

                var session = await _unitOfWork.SessionRepository.GetByIdAsync(record.SessionId, ct)
                    ?? throw new KeyNotFoundException("Session not found.");

                var trainingClass = await _unitOfWork.ClassRepository.GetByIdAsync(session.ClassId, ct);
                var isAssigned = trainingClass != null && _unitOfWork.ClassSubjectRepository.GetQueryable()
                    .Any(cs => cs.ClassId == trainingClass.ClassId && cs.SubjectId == session.SubjectId && cs.InstructorAccountId == instructorAccountId);
                ClassOwnershipValidator.EnsureInstructorOwnsSubject(roleName, isAssigned);

                record.InstructorSignedAt = DateTime.UtcNow;
                record.InstructorSignedByAccountId = instructorAccountId;
                if (!string.IsNullOrWhiteSpace(request?.Comments))
                {
                    record.InstructorComments = request.Comments;
                }
                record.UpdatedAt = DateTime.UtcNow;
                record.UpdatedByAccountId = instructorAccountId;

                _unitOfWork.AttendanceRecordRepository.Update(record);

                await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                {
                    AccountId = instructorAccountId,
                    ActionType = AuditActionType.UPDATE.ToString(),
                    EntityName = nameof(AttendanceRecord),
                    RecordId = id,
                    NewValue = $"InstructorSignedAt: {record.InstructorSignedAt:O}",
                    Description = $"Instructor #{instructorAccountId} digitally signed training attendance record #{id}"
                }, ct);

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return MapToResponse(record, session);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    public async Task<AttendanceRecordResponse> StudentSignOffAsync(int id, SignAttendanceRecordRequest request, int studentAccountId, string? roleName, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var record = await _unitOfWork.AttendanceRecordRepository.GetByIdAsync(id, ct)
                    ?? throw new KeyNotFoundException("AttendanceRecord not found.");

                var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(record.EnrollmentId, ct)
                    ?? throw new KeyNotFoundException("Enrollment not found.");

                // Zero-Trust: Students may only sign their own records (Admins must use admin-student-sign-override)
                if (enrollment.AccountId != studentAccountId)
                {
                    throw new ForbiddenAccessException("You are only authorized to sign your own training record.");
                }

                var session = await _unitOfWork.SessionRepository.GetByIdAsync(record.SessionId, ct);

                record.StudentSignedAt = DateTime.UtcNow;
                record.StudentSignedByAccountId = studentAccountId;
                if (!string.IsNullOrWhiteSpace(request?.Comments))
                {
                    record.StudentComments = request.Comments;
                }
                record.UpdatedAt = DateTime.UtcNow;
                record.UpdatedByAccountId = studentAccountId;

                _unitOfWork.AttendanceRecordRepository.Update(record);

                await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                {
                    AccountId = studentAccountId,
                    ActionType = AuditActionType.UPDATE.ToString(),
                    EntityName = nameof(AttendanceRecord),
                    RecordId = id,
                    NewValue = $"StudentSignedAt: {record.StudentSignedAt:O}",
                    Description = $"Student #{studentAccountId} digitally signed training attendance record #{id}"
                }, ct);

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return MapToResponse(record, session);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    public async Task<AttendanceRecordResponse> AdminStudentSignOverrideAsync(int id, AdminSignOverrideRequest request, int adminAccountId, string? roleName, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                if (!string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ForbiddenAccessException("Only administrators can perform proxy sign-off for students.");
                }

                if (request == null || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 10)
                {
                    throw new BusinessRuleViolationException("A valid reason with at least 10 characters is required for admin student sign-off override.");
                }

                var record = await _unitOfWork.AttendanceRecordRepository.GetByIdAsync(id, ct)
                    ?? throw new KeyNotFoundException("AttendanceRecord not found.");

                var enrollment = await _unitOfWork.CourseEnrollmentRepository.GetByIdAsync(record.EnrollmentId, ct)
                    ?? throw new KeyNotFoundException("Enrollment not found.");

                var session = await _unitOfWork.SessionRepository.GetByIdAsync(record.SessionId, ct);

                record.StudentSignedAt = DateTime.UtcNow;
                record.StudentSignedByAccountId = adminAccountId;
                string overrideNote = $"[Admin Override by Account #{adminAccountId}: {request.Reason.Trim()}]";
                record.StudentComments = string.IsNullOrWhiteSpace(record.StudentComments)
                    ? overrideNote
                    : $"{record.StudentComments}\n{overrideNote}";
                record.UpdatedAt = DateTime.UtcNow;
                record.UpdatedByAccountId = adminAccountId;

                _unitOfWork.AttendanceRecordRepository.Update(record);

                await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                {
                    AccountId = adminAccountId,
                    ActionType = AuditActionType.UPDATE.ToString(),
                    EntityName = nameof(AttendanceRecord),
                    RecordId = id,
                    NewValue = $"StudentSignedAt: {record.StudentSignedAt:O} (Admin Override by #{adminAccountId})",
                    Description = $"Admin #{adminAccountId} performed student sign override for Student Account #{enrollment.AccountId} on AttendanceRecord #{id}. Reason: {request.Reason.Trim()}"
                }, ct);

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return MapToResponse(record, session);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    private static void ValidateTrainingRecord(
        Session session,
        AttendanceStatus status,
        PerformanceGrade? performanceGrade,
        decimal? flightHours,
        decimal? simulatorHours,
        decimal? dualHours,
        decimal? soloHours,
        decimal? picHours,
        decimal? nightHours,
        decimal? instrumentHours,
        decimal? crossCountryHours,
        int? dayLandings,
        int? nightLandings,
        string? aircraftRegistration,
        string? simulatorDevice,
        string? departureIcao,
        string? arrivalIcao,
        string? route)
    {
        // 1. Negative checks
        if ((flightHours.HasValue && flightHours.Value < 0) ||
            (simulatorHours.HasValue && simulatorHours.Value < 0) ||
            (dualHours.HasValue && dualHours.Value < 0) ||
            (soloHours.HasValue && soloHours.Value < 0) ||
            (picHours.HasValue && picHours.Value < 0) ||
            (nightHours.HasValue && nightHours.Value < 0) ||
            (instrumentHours.HasValue && instrumentHours.Value < 0) ||
            (crossCountryHours.HasValue && crossCountryHours.Value < 0))
        {
            throw new BusinessRuleViolationException("Training hours cannot be negative.");
        }

        if ((dayLandings.HasValue && dayLandings.Value < 0) ||
            (nightLandings.HasValue && nightLandings.Value < 0))
        {
            throw new BusinessRuleViolationException("Landings cannot be negative.");
        }

        bool hasHours = (flightHours > 0) || (simulatorHours > 0) || (dualHours > 0) ||
                        (soloHours > 0) || (picHours > 0) || (nightHours > 0) ||
                        (instrumentHours > 0) || (crossCountryHours > 0);
        bool hasLandings = (dayLandings > 0) || (nightLandings > 0);

        // 2. Absent checks
        if (status == AttendanceStatus.Absent)
        {
            if (hasHours)
            {
                throw new BusinessRuleViolationException("Absent student cannot record training hours.");
            }
            if (hasLandings)
            {
                throw new BusinessRuleViolationException("Absent student cannot record landings.");
            }
            if (performanceGrade == PerformanceGrade.Satisfactory)
            {
                throw new BusinessRuleViolationException("Absent student cannot receive a Satisfactory grade.");
            }
        }

        // 3. Training Type specific checks
        switch (session.TrainingType)
        {
            case TrainingType.Theory:
                if (hasHours)
                {
                    throw new BusinessRuleViolationException("Theory sessions cannot record flight or simulator hours.");
                }
                if (hasLandings)
                {
                    throw new BusinessRuleViolationException("Theory sessions cannot record landings.");
                }
                if (!string.IsNullOrWhiteSpace(aircraftRegistration))
                {
                    throw new BusinessRuleViolationException("Theory sessions cannot record aircraft registration.");
                }
                if (!string.IsNullOrWhiteSpace(simulatorDevice))
                {
                    throw new BusinessRuleViolationException("Theory sessions cannot record simulator device.");
                }
                if (!string.IsNullOrWhiteSpace(departureIcao) || !string.IsNullOrWhiteSpace(arrivalIcao) || !string.IsNullOrWhiteSpace(route))
                {
                    throw new BusinessRuleViolationException("Theory sessions cannot record flight route details.");
                }
                break;

            case TrainingType.Flight:
                if (simulatorHours > 0)
                {
                    throw new BusinessRuleViolationException("Flight sessions cannot record simulator hours.");
                }
                if (!string.IsNullOrWhiteSpace(simulatorDevice))
                {
                    throw new BusinessRuleViolationException("Flight sessions cannot record simulator device.");
                }
                break;

            case TrainingType.Simulator:
                if (flightHours > 0)
                {
                    throw new BusinessRuleViolationException("Simulator sessions cannot record real flight hours.");
                }
                if (soloHours > 0)
                {
                    throw new BusinessRuleViolationException("Simulator sessions cannot record solo flight hours.");
                }
                if (picHours > 0)
                {
                    throw new BusinessRuleViolationException("Simulator sessions cannot record PIC flight hours.");
                }
                if (nightHours > 0)
                {
                    throw new BusinessRuleViolationException("Simulator sessions cannot record real night flight hours.");
                }
                if (crossCountryHours > 0)
                {
                    throw new BusinessRuleViolationException("Simulator sessions cannot record cross-country flight hours.");
                }
                if (dayLandings > 0 || nightLandings > 0)
                {
                    throw new BusinessRuleViolationException("Simulator sessions cannot record real aircraft landings.");
                }
                if (!string.IsNullOrWhiteSpace(aircraftRegistration))
                {
                    throw new BusinessRuleViolationException("Simulator sessions cannot record aircraft registration.");
                }
                if (!string.IsNullOrWhiteSpace(departureIcao) || !string.IsNullOrWhiteSpace(arrivalIcao) || !string.IsNullOrWhiteSpace(route))
                {
                    throw new BusinessRuleViolationException("Simulator sessions cannot record real flight route details.");
                }
                break;
        }
    }

    private static AttendanceRecordResponse MapToResponse(AttendanceRecord r, Session? session = null)
    {
        return new AttendanceRecordResponse(
            r.AttendanceRecordId,
            r.SessionId,
            r.EnrollmentId,
            r.Status,
            r.Remarks,
            r.RecordedByAccountId,
            r.RecordedAt,
            r.PerformanceGrade,
            r.FlightHours,
            r.SimulatorHours,
            r.DualHours,
            r.SoloHours,
            r.PicHours,
            r.NightHours,
            r.InstrumentHours,
            r.CrossCountryHours,
            r.DayLandings,
            r.NightLandings,
            r.AircraftRegistration,
            r.SimulatorDevice,
            r.DepartureIcao,
            r.ArrivalIcao,
            r.Route,
            r.InstructorComments,
            r.StudentComments,
            r.InstructorSignedAt,
            r.InstructorSignedByAccountId,
            r.StudentSignedAt,
            r.StudentSignedByAccountId,
            session?.TrainingType,
            session?.LessonCode
        );
    }
}
