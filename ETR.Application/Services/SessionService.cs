using System.ComponentModel.DataAnnotations;
using ETR.Application.Compliance;
using ETR.Application.DTOs.Session;
using ETR.Application.Exceptions;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Domain.Enums;

namespace ETR.Application.Services;

public class SessionService : ISessionService
{
    private readonly IUnitOfWork _unitOfWork;

    public SessionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<SessionResponse>> GetAllSessionsAsync(CancellationToken cancellationToken = default)
    {
        var sessions = await _unitOfWork.SessionRepository.GetAllAsync(cancellationToken);
        var assessments = await _unitOfWork.AssessmentRepository.GetAllAsync(cancellationToken);
        var checklists = await _unitOfWork.PracticalChecklistRepository.GetAllAsync(cancellationToken);
        var classes = await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken);
        var subjects = await _unitOfWork.SubjectRepository.GetAllAsync(cancellationToken);
        var classSubjects = await _unitOfWork.ClassSubjectRepository.GetAllAsync(cancellationToken);
        var facilities = await _unitOfWork.TrainingFacilityRepository.GetAllAsync(cancellationToken);
        var profiles = await _unitOfWork.UserProfileRepository.GetAllIncludingDeletedAsync(cancellationToken);
        var accounts = await _unitOfWork.AccountRepository.GetAllIncludingDeletedAsync(cancellationToken);

        return sessions.Select(s => {
            var cs = classSubjects.FirstOrDefault(x => x.ClassId == s.ClassId && x.SubjectId == s.SubjectId);
            string? instructorName = null;
            if (cs?.InstructorAccountId != null)
            {
                var p = profiles.FirstOrDefault(x => x.AccountId == cs.InstructorAccountId.Value);
                var a = accounts.FirstOrDefault(x => x.AccountId == cs.InstructorAccountId.Value);
                instructorName = p?.FullName ?? a?.Username ?? $"Instructor #{cs.InstructorAccountId.Value}";
            }
            return MapToResponse(
                s, 
                assessments.FirstOrDefault(a => a.AssessmentId == s.AssessmentId),
                checklists.FirstOrDefault(c => c.PracticalChecklistId == s.PracticalChecklistId),
                classes.FirstOrDefault(c => c.ClassId == s.ClassId),
                subjects.FirstOrDefault(sub => sub.SubjectId == s.SubjectId),
                cs,
                instructorName,
                facilities.FirstOrDefault(f => f.FacilityId == s.FacilityId)
            );
        }).ToList();
    }

    public async Task<IEnumerable<SessionResponse>> GetSessionsByClassIdAsync(int classId, CancellationToken cancellationToken = default)
    {
        var sessions = await _unitOfWork.SessionRepository.GetAllAsync(cancellationToken);
        var classSessions = sessions.Where(s => s.ClassId == classId).ToList();
        var assessments = await _unitOfWork.AssessmentRepository.GetAllAsync(cancellationToken);
        var checklists = await _unitOfWork.PracticalChecklistRepository.GetAllAsync(cancellationToken);
        var classes = await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken);
        var subjects = await _unitOfWork.SubjectRepository.GetAllAsync(cancellationToken);
        var classSubjects = await _unitOfWork.ClassSubjectRepository.GetAllAsync(cancellationToken);
        var facilities = await _unitOfWork.TrainingFacilityRepository.GetAllAsync(cancellationToken);
        var profiles = await _unitOfWork.UserProfileRepository.GetAllIncludingDeletedAsync(cancellationToken);
        var accounts = await _unitOfWork.AccountRepository.GetAllIncludingDeletedAsync(cancellationToken);

        return classSessions.Select(s => {
            var cs = classSubjects.FirstOrDefault(x => x.ClassId == s.ClassId && x.SubjectId == s.SubjectId);
            string? instructorName = null;
            if (cs?.InstructorAccountId != null)
            {
                var p = profiles.FirstOrDefault(x => x.AccountId == cs.InstructorAccountId.Value);
                var a = accounts.FirstOrDefault(x => x.AccountId == cs.InstructorAccountId.Value);
                instructorName = p?.FullName ?? a?.Username ?? $"Instructor #{cs.InstructorAccountId.Value}";
            }
            return MapToResponse(
                s, 
                assessments.FirstOrDefault(a => a.AssessmentId == s.AssessmentId),
                checklists.FirstOrDefault(c => c.PracticalChecklistId == s.PracticalChecklistId),
                classes.FirstOrDefault(c => c.ClassId == s.ClassId),
                subjects.FirstOrDefault(sub => sub.SubjectId == s.SubjectId),
                cs,
                instructorName,
                facilities.FirstOrDefault(f => f.FacilityId == s.FacilityId)
            );
        }).ToList();
    }

    public async Task<SessionResponse> GetSessionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var session = await _unitOfWork.SessionRepository.GetByIdAsync(id, cancellationToken);
        if (session == null || session.IsDeleted)
            throw new KeyNotFoundException($"Session with ID {id} not found.");

        Assessment? assessment = null;
        if (session.AssessmentId.HasValue)
        {
            assessment = await _unitOfWork.AssessmentRepository.GetByIdAsync(session.AssessmentId.Value, cancellationToken);
        }

        PracticalChecklist? checklist = null;
        if (session.PracticalChecklistId.HasValue)
        {
            checklist = await _unitOfWork.PracticalChecklistRepository.GetByIdAsync(session.PracticalChecklistId.Value, cancellationToken);
        }

        Class? cls = await _unitOfWork.ClassRepository.GetByIdAsync(session.ClassId, cancellationToken);
        Subject? subject = await _unitOfWork.SubjectRepository.GetByIdAsync(session.SubjectId, cancellationToken);
        TrainingFacility? facility = session.FacilityId.HasValue 
            ? await _unitOfWork.TrainingFacilityRepository.GetByIdAsync(session.FacilityId.Value, cancellationToken)
            : null;

        var cs = (await _unitOfWork.ClassSubjectRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(x => x.ClassId == session.ClassId && x.SubjectId == session.SubjectId);
        string? instructorName = null;
        if (cs?.InstructorAccountId != null)
        {
            var p = (await _unitOfWork.UserProfileRepository.GetAllIncludingDeletedAsync(cancellationToken))
                .FirstOrDefault(x => x.AccountId == cs.InstructorAccountId.Value);
            var a = (await _unitOfWork.AccountRepository.GetAllIncludingDeletedAsync(cancellationToken))
                .FirstOrDefault(x => x.AccountId == cs.InstructorAccountId.Value);
            instructorName = p?.FullName ?? a?.Username ?? $"Instructor #{cs.InstructorAccountId.Value}";
        }

        return MapToResponse(session, assessment, checklist, cls, subject, cs, instructorName, facility);
    }

    public async Task<SessionResponse> CreateSessionAsync(CreateSessionRequest request, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        // Clarified manual session workflow: Standard curriculum sessions are auto-provisioned.
        // Manual creation is reserved strictly for Remedial / Make-up / Retake sessions (IsRemedial = true).
        if (!request.IsRemedial)
        {
            throw new BusinessRuleViolationException(
                "Buổi học chính khóa được hệ thống tự động sinh theo chương trình đào tạo của khóa học và không thể tạo thủ công. " +
                "Chỉ có thể tạo thủ công các buổi học phụ đạo / học bù / thi lại (Remedial Session). Vui lòng tích chọn 'IsRemedial = true' để tạo buổi bổ sung.");
        }

        var cls = await _unitOfWork.ClassRepository.GetByIdAsync(request.ClassId, cancellationToken);
        if (cls == null || cls.IsDeleted)
            throw new KeyNotFoundException($"Class with ID {request.ClassId} not found.");

        var subject = await _unitOfWork.SubjectRepository.GetByIdAsync(request.SubjectId, cancellationToken);
        if (subject == null || subject.IsDeleted)
            throw new KeyNotFoundException($"Subject with ID {request.SubjectId} not found.");

        var classSubject = _unitOfWork.ClassSubjectRepository.GetQueryable()
            .FirstOrDefault(cs => cs.ClassId == request.ClassId && cs.SubjectId == request.SubjectId && !cs.IsDeleted);
        if (classSubject == null)
        {
            throw new ValidationException($"Môn học '{subject.SubjectName}' không thuộc lớp học '{cls.ClassCode}'.");
        }

        var (startAt, endAt) = ResolveTimeWindow(request.StartAt, request.EndAt, request.SessionDate);

        // Validate Facility Compatibility & Availability
        TrainingFacility? facility = null;
        if (request.FacilityId.HasValue)
        {
            facility = await ValidateFacilityCompatibilityAndAvailabilityAsync(
                request.FacilityId.Value, 
                request.TrainingType, 
                subject, 
                startAt, 
                endAt, 
                excludeSessionId: null, 
                cancellationToken);
        }

        // Validate Instructor Conflict
        if (classSubject.InstructorAccountId.HasValue)
        {
            ValidateInstructorConflict(classSubject.InstructorAccountId.Value, startAt, endAt, excludeSessionId: null);
        }

        var session = new Session
        {
            ClassId = request.ClassId,
            SubjectId = request.SubjectId,
            SessionTitle = request.SessionTitle.Trim(),
            SessionDate = startAt,
            StartAt = startAt,
            EndAt = endAt,
            FacilityId = request.FacilityId,
            Location = facility?.FacilityName ?? request.Location?.Trim(),
            IsAssessmentRequired = request.IsAssessmentRequired,
            IsChecklistRequired = request.IsChecklistRequired,
            AssessmentId = request.AssessmentId,
            PracticalChecklistId = request.PracticalChecklistId,
            TrainingType = request.TrainingType,
            LessonCode = string.IsNullOrWhiteSpace(request.LessonCode) ? null : request.LessonCode.Trim(),
            IsRemedial = true,
            IsConfirmed = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _unitOfWork.SessionRepository.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = createdByAccountId,
            ActionType = AuditActionType.INSERT.ToString(),
            EntityName = nameof(Session),
            RecordId = session.SessionId,
            NewValue = session.SessionTitle,
            Description = $"Created remedial session #{session.SessionId} ('{session.SessionTitle}') for class '{cls.ClassCode}'"
        }, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return await GetSessionByIdAsync(session.SessionId, cancellationToken);
    }

    public async Task<SessionResponse> UpdateSessionAsync(int id, UpdateSessionRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var session = await _unitOfWork.SessionRepository.GetByIdAsync(id, cancellationToken);
        if (session == null || session.IsDeleted)
            throw new KeyNotFoundException($"Session with ID {id} not found.");

        var effectiveTrainingType = request.TrainingType ?? session.TrainingType;
        var subject = await _unitOfWork.SubjectRepository.GetByIdAsync(session.SubjectId, cancellationToken);

        Assessment? assessment = null;
        if (request.AssessmentId.HasValue)
        {
            var classExists = await _unitOfWork.ClassRepository.GetByIdAsync(session.ClassId, cancellationToken);
            assessment = await _unitOfWork.AssessmentRepository.GetByIdAsync(request.AssessmentId.Value, cancellationToken);
            if (assessment == null)
                throw new ValidationException($"Assessment with ID {request.AssessmentId.Value} does not exist.");
            if (assessment.CourseId != classExists?.CourseId || assessment.SubjectId != session.SubjectId)
                throw new ValidationException("Assessment does not match the class's course or the specified subject.");

            // Validate TrainingType compatibility:
            if (string.Equals(assessment.AssessmentType, "Theory", StringComparison.OrdinalIgnoreCase) &&
                effectiveTrainingType != TrainingType.Theory)
            {
                throw new ValidationException($"Không thể gán bài thi lý thuyết (Theory Assessment: '{assessment.ComponentName}') vào buổi huấn luyện thực hành ({effectiveTrainingType}). Buổi thực hành bay hoặc buồng lái mô phỏng phải sử dụng Practical Checklist để đánh giá năng lực thực hành.");
            }
        }

        PracticalChecklist? checklist = null;
        if (request.PracticalChecklistId.HasValue)
        {
            var classExists = await _unitOfWork.ClassRepository.GetByIdAsync(session.ClassId, cancellationToken);
            checklist = await _unitOfWork.PracticalChecklistRepository.GetByIdAsync(request.PracticalChecklistId.Value, cancellationToken);
            if (checklist == null)
                throw new ValidationException($"PracticalChecklist with ID {request.PracticalChecklistId.Value} does not exist.");
            if (checklist.CourseId != classExists?.CourseId || checklist.SubjectId != session.SubjectId)
                throw new ValidationException("PracticalChecklist does not match the class's course or the specified subject.");
        }

        var (startAt, endAt) = ResolveTimeWindow(
            request.StartAt ?? (request.SessionDate != default ? request.SessionDate : session.StartAt),
            request.EndAt ?? session.EndAt,
            request.SessionDate != default ? request.SessionDate : (session.SessionDate ?? DateTime.UtcNow)
        );

        // Validate Facility Compatibility & Conflict
        TrainingFacility? facility = null;
        var targetFacilityId = request.FacilityId ?? session.FacilityId;

        // If explicitly set to null (or provided new id), validate
        if (request.FacilityId.HasValue)
        {
            facility = await ValidateFacilityCompatibilityAndAvailabilityAsync(
                request.FacilityId.Value,
                effectiveTrainingType,
                subject,
                startAt,
                endAt,
                excludeSessionId: id,
                cancellationToken);
        }
        else if (targetFacilityId.HasValue)
        {
            // If facility wasn't changed but time or training type changed, re-verify availability & compatibility
            facility = await ValidateFacilityCompatibilityAndAvailabilityAsync(
                targetFacilityId.Value,
                effectiveTrainingType,
                subject,
                startAt,
                endAt,
                excludeSessionId: id,
                cancellationToken);
        }

        // Validate Instructor Conflict
        var classSubject = _unitOfWork.ClassSubjectRepository.GetQueryable()
            .FirstOrDefault(cs => cs.ClassId == session.ClassId && cs.SubjectId == session.SubjectId && !cs.IsDeleted);
        if (classSubject?.InstructorAccountId != null)
        {
            ValidateInstructorConflict(classSubject.InstructorAccountId.Value, startAt, endAt, excludeSessionId: id);
        }

        // Update fields
        if (!string.IsNullOrWhiteSpace(request.SessionTitle))
            session.SessionTitle = request.SessionTitle.Trim();

        session.SessionDate = startAt;
        session.StartAt = startAt;
        session.EndAt = endAt;

        if (request.FacilityId.HasValue)
        {
            session.FacilityId = request.FacilityId.Value;
            session.Location = facility?.FacilityName ?? request.Location?.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(request.Location))
        {
            session.Location = request.Location.Trim();
            // If location was edited manually as text outside facility catalog
            if (request.FacilityId == null && session.FacilityId.HasValue && session.Location != facility?.FacilityName)
            {
                session.FacilityId = null;
            }
        }

        session.IsAssessmentRequired = request.IsAssessmentRequired;
        session.IsChecklistRequired = request.IsChecklistRequired;
        session.AssessmentId = request.AssessmentId;
        session.PracticalChecklistId = request.PracticalChecklistId;

        if (request.TrainingType.HasValue)
            session.TrainingType = request.TrainingType.Value;

        if (request.LessonCode != null)
            session.LessonCode = string.IsNullOrWhiteSpace(request.LessonCode) ? null : request.LessonCode.Trim();

        if (request.IsRemedial.HasValue)
            session.IsRemedial = request.IsRemedial.Value;

        session.UpdatedAt = DateTime.UtcNow;
        session.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.SessionRepository.Update(session);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = updatedByAccountId,
            ActionType = AuditActionType.UPDATE.ToString(),
            EntityName = nameof(Session),
            RecordId = id,
            NewValue = $"Title: {session.SessionTitle}, Type: {session.TrainingType}, FacilityId: {session.FacilityId}, Time: {session.StartAt:yyyy-MM-dd HH:mm} - {session.EndAt:HH:mm}",
            Description = $"Updated session schedule #{id} ('{session.SessionTitle}')"
        }, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return await GetSessionByIdAsync(id, cancellationToken);
    }

    public async Task DeleteSessionAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var session = await _unitOfWork.SessionRepository.GetByIdAsync(id, cancellationToken);
        if (session == null || session.IsDeleted)
            throw new KeyNotFoundException($"Session with ID {id} not found.");

        if (!session.IsRemedial)
        {
            throw new BusinessRuleViolationException("Buổi học chính khóa được sinh tự động theo khung chương trình và không được phép xóa. Chỉ có thể xóa buổi học phụ đạo / học bù (Remedial Session).");
        }

        session.IsDeleted = true;
        session.DeletedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        session.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.SessionRepository.Update(session);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = deletedByAccountId,
            ActionType = AuditActionType.DELETE.ToString(),
            EntityName = nameof(Session),
            RecordId = id,
            Description = $"Deleted remedial session #{id} ('{session.SessionTitle}')"
        }, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
    }

    private static (DateTime startAt, DateTime endAt) ResolveTimeWindow(DateTime? requestedStart, DateTime? requestedEnd, DateTime fallbackDate)
    {
        DateTime start;
        DateTime end;

        if (requestedStart.HasValue && requestedEnd.HasValue)
        {
            start = requestedStart.Value;
            end = requestedEnd.Value;
        }
        else if (requestedStart.HasValue)
        {
            start = requestedStart.Value;
            end = start.AddHours(2);
        }
        else
        {
            start = fallbackDate != default ? fallbackDate : DateTime.UtcNow;
            end = start.AddHours(2);
        }

        if (end <= start)
        {
            throw new ValidationException("Thời gian kết thúc (EndAt) phải sau thời gian bắt đầu (StartAt).");
        }

        return (start, end);
    }

    private async Task<TrainingFacility> ValidateFacilityCompatibilityAndAvailabilityAsync(
        int facilityId,
        TrainingType trainingType,
        Subject? subject,
        DateTime startAt,
        DateTime endAt,
        int? excludeSessionId,
        CancellationToken cancellationToken)
    {
        var facility = await _unitOfWork.TrainingFacilityRepository.GetByIdAsync(facilityId, cancellationToken);
        if (facility == null || facility.IsDeleted || !facility.IsActive)
        {
            throw new ValidationException($"Cơ sở đào tạo với ID {facilityId} không tồn tại hoặc đã ngừng hoạt động.");
        }

        bool isWorkshopOrPractical = FacilityCompatibilityHelper.IsWorkshopOrPracticalSubject(
            subject?.SubjectCode, subject?.SubjectName, subject?.SubjectType);

        if (!FacilityCompatibilityHelper.IsCompatible(trainingType, isWorkshopOrPractical, facility.FacilityType))
        {
            var compatList = string.Join(", ", FacilityCompatibilityHelper.GetCompatibleFacilityTypes(trainingType, isWorkshopOrPractical));
            throw new ValidationException(
                $"Cơ sở đào tạo '{facility.FacilityName}' ({facility.FacilityType}) không tương thích với buổi đào tạo {trainingType} của môn '{subject?.SubjectName}'. " +
                $"Loại cơ sở đào tạo yêu cầu: {compatList}.");
        }

        // Conflict check against other sessions booking this facility
        var conflictingSessions = _unitOfWork.SessionRepository.GetQueryable()
            .Where(s => s.FacilityId == facilityId && !s.IsDeleted && (!excludeSessionId.HasValue || s.SessionId != excludeSessionId.Value))
            .ToList();

        foreach (var cs in conflictingSessions)
        {
            DateTime csStart = cs.StartAt ?? cs.SessionDate ?? DateTime.MinValue;
            DateTime csEnd = cs.EndAt ?? (cs.SessionDate.HasValue ? cs.SessionDate.Value.AddHours(2) : csStart.AddHours(2));

            if (FacilityCompatibilityHelper.HasTimeOverlap(startAt, endAt, csStart, csEnd))
            {
                var confClass = _unitOfWork.ClassRepository.GetQueryable().FirstOrDefault(c => c.ClassId == cs.ClassId);
                throw new ValidationException(
                    $"Xung đột cơ sở vật chất: Cơ sở '{facility.FacilityName}' ({facility.FacilityCode}) đã được xếp lịch cho lớp '{confClass?.ClassCode ?? cs.ClassId.ToString()}', buổi '{cs.SessionTitle}' " +
                    $"từ {csStart:HH:mm dd/MM/yyyy} đến {csEnd:HH:mm dd/MM/yyyy}. Vui lòng chọn khung giờ khác hoặc phòng khác.");
            }
        }

        return facility;
    }

    private void ValidateInstructorConflict(
        int instructorAccountId,
        DateTime startAt,
        DateTime endAt,
        int? excludeSessionId)
    {
        var instructorClassSubjects = _unitOfWork.ClassSubjectRepository.GetQueryable()
            .Where(cs => cs.InstructorAccountId == instructorAccountId && !cs.IsDeleted)
            .ToList();

        var instructorKeys = instructorClassSubjects
            .Select(cs => (cs.ClassId, cs.SubjectId))
            .ToHashSet();

        var otherSessions = _unitOfWork.SessionRepository.GetQueryable()
            .Where(s => !s.IsDeleted && (!excludeSessionId.HasValue || s.SessionId != excludeSessionId.Value))
            .ToList()
            .Where(s => instructorKeys.Contains((s.ClassId, s.SubjectId)))
            .ToList();

        foreach (var os in otherSessions)
        {
            DateTime osStart = os.StartAt ?? os.SessionDate ?? DateTime.MinValue;
            DateTime osEnd = os.EndAt ?? (os.SessionDate.HasValue ? os.SessionDate.Value.AddHours(2) : osStart.AddHours(2));

            if (FacilityCompatibilityHelper.HasTimeOverlap(startAt, endAt, osStart, osEnd))
            {
                var confClass = _unitOfWork.ClassRepository.GetQueryable().FirstOrDefault(c => c.ClassId == os.ClassId);
                throw new ValidationException(
                    $"Xung đột lịch giảng viên: Giảng viên đã có lịch giảng dạy lớp '{confClass?.ClassCode ?? os.ClassId.ToString()}', buổi '{os.SessionTitle}' " +
                    $"từ {osStart:HH:mm dd/MM/yyyy} đến {osEnd:HH:mm dd/MM/yyyy}.");
            }
        }
    }

    private static SessionResponse MapToResponse(
        Session session, 
        Assessment? assessment = null, 
        PracticalChecklist? practicalChecklist = null, 
        Class? cls = null, 
        Subject? subject = null,
        ClassSubject? classSubject = null,
        string? instructorName = null,
        TrainingFacility? facility = null)
    {
        return new SessionResponse
        {
            SessionId = session.SessionId,
            ClassId = session.ClassId,
            ClassCode = cls?.ClassCode ?? string.Empty,
            ClassName = cls?.ClassName ?? string.Empty,
            SubjectId = session.SubjectId,
            SubjectName = subject?.SubjectName ?? string.Empty,
            SessionTitle = session.SessionTitle,
            SessionDate = session.SessionDate,
            StartAt = session.StartAt ?? session.SessionDate,
            EndAt = session.EndAt ?? (session.SessionDate.HasValue ? session.SessionDate.Value.AddHours(2) : null),
            Location = session.Location,
            FacilityId = session.FacilityId,
            FacilityCode = facility?.FacilityCode,
            FacilityName = facility?.FacilityName,
            FacilityType = facility?.FacilityType,
            IsRemedial = session.IsRemedial,
            IsConfirmed = session.IsConfirmed,
            ConfirmedByAccountId = session.ConfirmedByAccountId,
            ConfirmedAt = session.ConfirmedAt,
            IsAssessmentRequired = session.IsAssessmentRequired,
            IsChecklistRequired = session.IsChecklistRequired,
            AssessmentId = session.AssessmentId,
            Assessment = assessment != null ? new ETR.Application.DTOs.Assessment.Responses.AssessmentResponse(
                assessment.AssessmentId, assessment.CourseId, assessment.SubjectId, assessment.ComponentName,
                assessment.AssessmentType, assessment.Weight, assessment.PassingScore, assessment.IsRequired,
                assessment.DisplayOrder) : null,
            PracticalChecklistId = session.PracticalChecklistId,
            PracticalChecklist = practicalChecklist != null ? new ETR.Application.DTOs.PracticalChecklist.PracticalChecklistResponse
            {
                PracticalChecklistId = practicalChecklist.PracticalChecklistId,
                CourseId = practicalChecklist.CourseId,
                SubjectId = practicalChecklist.SubjectId,
                ItemName = practicalChecklist.ItemName,
                Description = practicalChecklist.Description,
                IsRequired = practicalChecklist.IsRequired,
                DisplayOrder = practicalChecklist.DisplayOrder
            } : null,
            TrainingType = session.TrainingType,
            LessonCode = session.LessonCode,
            InstructorAccountId = classSubject?.InstructorAccountId,
            InstructorName = instructorName
        };
    }
}
