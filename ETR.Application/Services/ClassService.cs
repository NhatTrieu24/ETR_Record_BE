using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Domain.Enums;

namespace ETR.Application.Services;

public class ClassService : IClassService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ClassService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<TrainingClassResponse>> GetAllClassesAsync(CancellationToken cancellationToken = default)
    {
        var classes = await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken);
        var visible = classes.Where(c => !c.IsDeleted).ToList();

        // "Sân nhà ai nấy đá" (team decision 2026-08-08, docs/todo/addition.md): Instructor only
        // sees classes they are actually assigned to, not the whole system's class list.
        if (string.Equals(_currentUserService.RoleName, "Instructor", StringComparison.OrdinalIgnoreCase) && _currentUserService.AccountId.HasValue)
        {
            var myClassIds = _unitOfWork.ClassSubjectRepository.GetQueryable()
                .Where(cs => cs.InstructorAccountId == _currentUserService.AccountId.Value)
                .Select(cs => cs.ClassId)
                .ToHashSet();
            
            visible = visible.Where(c => myClassIds.Contains(c.ClassId)).ToList();
        }
        
        var allClassSubjects = await _unitOfWork.ClassSubjectRepository.GetAllAsync(cancellationToken);
        var allProfiles = await _unitOfWork.UserProfileRepository.GetAllIncludingDeletedAsync(cancellationToken);
        var allAccounts = await _unitOfWork.AccountRepository.GetAllIncludingDeletedAsync(cancellationToken);

        return visible.Select(c => {
            var assignments = allClassSubjects
                .Where(cs => cs.ClassId == c.ClassId)
                .Select(cs => new InstructorAssignmentResponse(
                    cs.ClassSubjectId, 
                    cs.SubjectId, 
                    cs.InstructorAccountId,
                    ResolveInstructorName(cs.InstructorAccountId, allProfiles, allAccounts)))
                .ToList();

            return new TrainingClassResponse(
                c.ClassId, c.ClassCode, c.ClassName, c.CourseId, c.StartDate, c.EndDate, c.Location, c.Capacity, c.Status, assignments, c.CourseVersionNo);
        });
    }

    public async Task<TrainingClassResponse> GetClassByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var c = await _unitOfWork.ClassRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Class not found.");

        if (c.IsDeleted) throw new KeyNotFoundException("Class not found.");

        var allClassSubjects = await _unitOfWork.ClassSubjectRepository.GetAllAsync(cancellationToken);
        var allProfiles = await _unitOfWork.UserProfileRepository.GetAllIncludingDeletedAsync(cancellationToken);
        var allAccounts = await _unitOfWork.AccountRepository.GetAllIncludingDeletedAsync(cancellationToken);
        var assignments = allClassSubjects
            .Where(cs => cs.ClassId == c.ClassId)
            .Select(cs => new InstructorAssignmentResponse(
                cs.ClassSubjectId, 
                cs.SubjectId, 
                cs.InstructorAccountId,
                ResolveInstructorName(cs.InstructorAccountId, allProfiles, allAccounts)))
            .ToList();

        return new TrainingClassResponse(c.ClassId, c.ClassCode, c.ClassName, c.CourseId, c.StartDate, c.EndDate, c.Location, c.Capacity, c.Status, assignments, c.CourseVersionNo);
    }

    public async Task<TrainingClassResponse> CreateClassAsync(CreateClassRequest request, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var result = await CreateClassCoreAsync(request, createdByAccountId, ct);
                await _unitOfWork.CommitTransactionAsync(ct);
                return result;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    public async Task<TrainingClassResponse> CreateClassCoreAsync(CreateClassRequest request, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        var ct = cancellationToken;

        var course = await _unitOfWork.CourseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new BusinessRuleViolationException("Course not found.");

        if (course.Status != CourseStatus.Active)
        {
            throw new BusinessRuleViolationException($"Không thể mở lớp học mới cho khóa học ở trạng thái '{course.Status}'. Chỉ có thể mở lớp cho khóa học đang Hoạt động (Active).");
        }

        var isDuplicate = _unitOfWork.ClassRepository.GetQueryable()
            .Any(c => c.ClassCode == request.ClassCode && !c.IsDeleted);
        if (isDuplicate)
        {
            throw new BusinessRuleViolationException($"A class with code '{request.ClassCode}' already exists.");
        }

        if (AcademyTimeHelper.IsInPast(request.StartDate))
        {
            throw new BusinessRuleViolationException("Ngày bắt đầu đào tạo không được ở trong quá khứ.");
        }

        if (request.EndDate <= request.StartDate)
        {
            throw new BusinessRuleViolationException("Ngày kết thúc phải sau ngày bắt đầu.");
        }

        // Lấy toàn bộ môn học thuộc khóa học (CourseSubjects) để kiểm tra thời lượng đào tạo chuẩn ICAO
        var courseSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(ct))
            .Where(x => x.CourseId == request.CourseId).ToList();

        var subjectMap = (await _unitOfWork.SubjectRepository.GetAllAsync(ct))
            .ToDictionary(s => s.SubjectId, s => s.SubjectType);

        var subjectPairs = courseSubjects.Select(cs => (
            cs.RequiredHours,
            subjectMap.TryGetValue(cs.SubjectId, out var st) ? st : null
        ));

        var (minTrainingDays, minBufferDays, totalMinDays, minEndDate) =
            ClassDurationValidator.CalculateMinDuration(request.StartDate, subjectPairs);

        if (request.EndDate.Date < minEndDate.Date)
        {
            throw new BusinessRuleViolationException(
                $"Thời gian kết thúc quá ngắn so với tổng số giờ học chuẩn ICAO/CAAV. Lớp học yêu cầu tối thiểu {totalMinDays} ngày đào tạo (kết thúc từ ngày {minEndDate:dd/MM/yyyy}, bao gồm {minTrainingDays} ngày học và {minBufferDays} ngày đệm).");
        }

        var cls = new Class
        {
            ClassCode = request.ClassCode,
            ClassName = request.ClassName,
            CourseId = request.CourseId,
            CourseVersionNo = course.VersionNo,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Location = request.Location,
            Capacity = request.Capacity,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _unitOfWork.ClassRepository.AddAsync(cls, ct);
        await _unitOfWork.SaveAsync(ct);

        var assignments = new List<InstructorAssignmentResponse>();
        var classSubjects = new List<ClassSubject>();
        var sessions = new List<Session>();

        var assignmentDict = request.InstructorAssignments?
            .Where(a => a.InstructorAccountId.HasValue)
            .ToDictionary(a => a.SubjectId, a => a.InstructorAccountId) ?? new Dictionary<int, int?>();

        // 1. Tạo ClassSubject cho tất cả các môn trong khóa học
        foreach (var cs in courseSubjects)
        {
            int? instructorId = assignmentDict.TryGetValue(cs.SubjectId, out var id) ? id : null;
            if (instructorId.HasValue)
            {
                await EnsureAccountHasInstructorRoleAsync(instructorId.Value, ct);
            }

            var classSubject = new ClassSubject
            {
                ClassId = cls.ClassId,
                SubjectId = cs.SubjectId,
                InstructorAccountId = instructorId
            };
            classSubjects.Add(classSubject);
            await _unitOfWork.ClassSubjectRepository.AddAsync(classSubject, ct);
        }
        await _unitOfWork.SaveAsync(ct);

        // 2. Tự động tạo sẵn danh sách Sessions (Buổi học) theo chuẩn ICAO và SubjectType
        sessions = await GenerateSessionsForClassCoreAsync(cls, courseSubjects, subjectMap, request.Location, ct);
        await _unitOfWork.SaveAsync(ct);

        var allProfiles = await _unitOfWork.UserProfileRepository.GetAllIncludingDeletedAsync(ct);
        var allAccounts = await _unitOfWork.AccountRepository.GetAllIncludingDeletedAsync(ct);
        assignments = classSubjects.Select(x => new InstructorAssignmentResponse(
            x.ClassSubjectId, 
            x.SubjectId, 
            x.InstructorAccountId,
            ResolveInstructorName(x.InstructorAccountId, allProfiles, allAccounts))).ToList();

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = createdByAccountId,
            ActionType = AuditActionType.INSERT.ToString(),
            EntityName = nameof(Class),
            RecordId = cls.ClassId,
            NewValue = cls.ClassCode,
            Description = $"Class #{cls.ClassId} ({cls.ClassCode}) created with {classSubjects.Count} subjects and {sessions.Count} sessions"
        }, ct);

        await _unitOfWork.SaveAsync(ct);

        return new TrainingClassResponse(cls.ClassId, cls.ClassCode, cls.ClassName, cls.CourseId, cls.StartDate, cls.EndDate, cls.Location, cls.Capacity, cls.Status, assignments, cls.CourseVersionNo);
    }

    public async Task<TrainingClassResponse> UpdateClassAsync(int id, UpdateClassRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var cls = await _unitOfWork.ClassRepository.GetByIdAsync(id, ct)
                    ?? throw new KeyNotFoundException("Class not found.");

                if (cls.IsDeleted) throw new KeyNotFoundException("Class not found.");

                var isDuplicate = _unitOfWork.ClassRepository.GetQueryable()
                    .Any(c => c.ClassId != id && c.ClassCode == request.ClassCode && !c.IsDeleted);
                if (isDuplicate)
                {
                    throw new BusinessRuleViolationException($"A class with code '{request.ClassCode}' already exists.");
                }

                var oldStatus = cls.Status;

                if (request.Status == ClassStatus.Completed && oldStatus != ClassStatus.Completed)
                {
                    var unconfirmedSessions = _unitOfWork.SessionRepository.GetQueryable()
                        .Any(s => s.ClassId == id && !s.IsConfirmed && !s.IsDeleted);
                    if (unconfirmedSessions)
                    {
                        throw new BusinessRuleViolationException("Cannot mark class as Completed because there are still unconfirmed sessions.");
                    }
                }

                cls.ClassCode = request.ClassCode;
                cls.ClassName = request.ClassName;
                cls.CourseId = request.CourseId; // Although this shouldn't normally change
                cls.StartDate = request.StartDate;
                cls.EndDate = request.EndDate;
                cls.Location = request.Location;
                cls.Capacity = request.Capacity;
                cls.Status = request.Status;
                cls.UpdatedAt = DateTime.UtcNow;
                cls.UpdatedByAccountId = updatedByAccountId;

                _unitOfWork.ClassRepository.Update(cls);

                // Update ClassSubjects
                var existingAssignments = _unitOfWork.ClassSubjectRepository.GetQueryable().Where(x => x.ClassId == cls.ClassId).ToList();
                var existingDict = existingAssignments
                    .GroupBy(x => x.SubjectId)
                    .ToDictionary(g => g.Key, g => g.First().InstructorAccountId);

                foreach (var ea in existingAssignments)
                {
                    _unitOfWork.ClassSubjectRepository.Delete(ea);
                }
                await _unitOfWork.SaveAsync(ct); // Clear existing

                var assignments = new List<InstructorAssignmentResponse>();
                var classSubjects = new List<ClassSubject>();
                var courseSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(ct))
                    .Where(x => x.CourseId == request.CourseId).ToList();

                // If request.InstructorAssignments is null (caller only updated general class info), preserve existing assignments
                var assignmentDict = request.InstructorAssignments != null
                    ? request.InstructorAssignments
                        .Where(a => a.InstructorAccountId.HasValue)
                        .GroupBy(a => a.SubjectId)
                        .ToDictionary(g => g.Key, g => g.First().InstructorAccountId)
                    : existingDict;

                // 1. Gán lại ClassSubject cho tất cả môn học trong khóa
                foreach (var cs in courseSubjects)
                {
                    int? instructorId = assignmentDict.TryGetValue(cs.SubjectId, out var idVal) ? idVal : null;
                    if (instructorId.HasValue)
                    {
                        await EnsureAccountHasInstructorRoleAsync(instructorId.Value, ct);
                    }

                    var classSubject = new ClassSubject
                    {
                        ClassId = cls.ClassId,
                        SubjectId = cs.SubjectId,
                        InstructorAccountId = instructorId
                    };
                    classSubjects.Add(classSubject);
                    await _unitOfWork.ClassSubjectRepository.AddAsync(classSubject, ct);
                }
                await _unitOfWork.SaveAsync(ct);

                // 2. Tự động sinh Sessions:
                // - Nếu lớp chưa có Session nào: sinh toàn bộ Sessions cho các môn của khóa học
                // - Nếu lớp đã có Session nhưng khóa học có thêm môn mới:
                //   CHỈ sinh thêm Session cho môn mới nếu lớp CHƯA CÓ HỌC VIÊN ENROLL (đúng nghiệp vụ bảo vệ tiến độ ETR).
                var existingSessions = (await _unitOfWork.SessionRepository.GetAllAsync(ct))
                    .Where(s => s.ClassId == cls.ClassId && !s.IsDeleted).ToList();
                var existingSessionSubjectIds = existingSessions.Select(s => s.SubjectId).ToHashSet();
                var missingSubjects = courseSubjects.Where(cs => !existingSessionSubjectIds.Contains(cs.SubjectId)).ToList();

                if (missingSubjects.Any())
                {
                    var allEnrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(ct);
                    bool hasEnrolledStudents = allEnrollments.Any(e => e.ClassId == cls.ClassId && !e.IsDeleted && e.Status != "Withdrawn" && e.Status != "Deleted");

                    if (!existingSessions.Any() || !hasEnrolledStudents)
                    {
                        var subjectMap = (await _unitOfWork.SubjectRepository.GetAllAsync(ct))
                            .ToDictionary(s => s.SubjectId, s => s.SubjectType);
                        await GenerateSessionsForClassCoreAsync(cls, courseSubjects, subjectMap, request.Location, ct);
                        await _unitOfWork.SaveAsync(ct);
                    }
                }

                var allProfiles = await _unitOfWork.UserProfileRepository.GetAllIncludingDeletedAsync(ct);
                var allAccounts = await _unitOfWork.AccountRepository.GetAllIncludingDeletedAsync(ct);
                assignments = classSubjects.Select(x => new InstructorAssignmentResponse(
                    x.ClassSubjectId, 
                    x.SubjectId, 
                    x.InstructorAccountId,
                    ResolveInstructorName(x.InstructorAccountId, allProfiles, allAccounts))).ToList();

                await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                {
                    AccountId = updatedByAccountId,
                    ActionType = AuditActionType.UPDATE.ToString(),
                    EntityName = nameof(Class),
                    RecordId = cls.ClassId,
                    OldValue = oldStatus.ToString(),
                    NewValue = cls.Status.ToString(),
                    Description = $"Class #{cls.ClassId} ({cls.ClassCode}) updated"
                }, ct);

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return new TrainingClassResponse(cls.ClassId, cls.ClassCode, cls.ClassName, cls.CourseId, cls.StartDate, cls.EndDate, cls.Location, cls.Capacity, cls.Status, assignments, cls.CourseVersionNo);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    public async Task DeleteClassAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var cls = await _unitOfWork.ClassRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Class not found.");

        if (cls.IsDeleted) return;

        if (cls.Status == ClassStatus.InProgress || cls.Status == ClassStatus.Completed)
        {
            throw new BusinessRuleViolationException($"Không thể xóa lớp học đang ở trạng thái '{cls.Status}'. Vui lòng đổi trạng thái lớp sang 'Đã hủy' (Cancelled) thay vì xóa.");
        }

        var hasActiveEnrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken))
            .Any(e => e.ClassId == id && !e.IsDeleted && e.Status != EnrollmentStatus.Withdrawn && e.Status != EnrollmentStatus.Deleted);
        if (hasActiveEnrollments)
        {
            throw new BusinessRuleViolationException($"Không thể xóa lớp '{cls.ClassName}' vì đã có học viên ghi danh. Vui lòng hủy ghi danh học viên hoặc đổi trạng thái lớp sang 'Đã hủy' (Cancelled).");
        }

        // Soft Delete
        cls.IsDeleted = true;
        cls.DeletedAt = DateTime.UtcNow;
        cls.UpdatedAt = DateTime.UtcNow;
        cls.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.ClassRepository.Update(cls);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = deletedByAccountId,
            ActionType = AuditActionType.DELETE.ToString(),
            EntityName = nameof(Class),
            RecordId = cls.ClassId,
            OldValue = cls.Status.ToString(),
            NewValue = "Deleted",
            Description = $"Class #{cls.ClassId} ({cls.ClassCode}) deleted"
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);
    }

    private async Task EnsureAccountHasInstructorRoleAsync(int instructorAccountId, CancellationToken cancellationToken)
    {
        var account = await _unitOfWork.AccountRepository.GetByIdAsync(instructorAccountId, cancellationToken)
            ?? throw new BusinessRuleViolationException($"Account (ID: {instructorAccountId}) not found.");

        var role = await _unitOfWork.RoleRepository.GetByIdAsync(account.RoleId, cancellationToken);
        if (role == null || role.RoleName != "Instructor")
        {
            throw new BusinessRuleViolationException("InstructorAccountId must reference an account with the Instructor role.");
        }
    }

    private async Task<List<Session>> GenerateSessionsForClassCoreAsync(
        Class cls,
        List<CourseSubject> courseSubjects,
        IReadOnlyDictionary<int, string> subjectMap,
        string? classLocation,
        CancellationToken ct)
    {
        var sessions = new List<Session>();

        var allSubjects = (await _unitOfWork.SubjectRepository.GetAllAsync(ct)).ToList();

        // Tải danh sách Assessments và PracticalChecklists thuộc khóa học để tự động gắn vào các buổi kiểm tra
        var assessments = (await _unitOfWork.AssessmentRepository.GetAllAsync(ct))
            .Where(a => a.CourseId == cls.CourseId && !a.IsDeleted)
            .ToList();

        var checklists = (await _unitOfWork.PracticalChecklistRepository.GetAllAsync(ct))
            .Where(pc => (pc.CourseId == cls.CourseId || pc.CourseId == 0) && !pc.IsDeleted)
            .ToList();

        var existingSessions = (await _unitOfWork.SessionRepository.GetAllAsync(ct))
            .Where(s => s.ClassId == cls.ClassId && !s.IsDeleted).ToList();
        var subjectsWithExistingSessions = existingSessions.Select(s => s.SubjectId).ToHashSet();

        // Chỉ lọc các môn chưa có Session trong lớp
        var missingCourseSubjects = courseSubjects
            .Where(cs => !subjectsWithExistingSessions.Contains(cs.SubjectId))
            .OrderBy(cs => cs.SequenceNo)
            .ToList();

        if (!missingCourseSubjects.Any()) return sessions;

        var currentDate = cls.StartDate.Date;
        if (existingSessions.Any(s => s.SessionDate.HasValue))
        {
            var maxExistingDate = existingSessions.Where(s => s.SessionDate.HasValue).Max(s => s.SessionDate!.Value.Date);
            currentDate = maxExistingDate.AddDays(1);
            if (currentDate.DayOfWeek == DayOfWeek.Sunday) currentDate = currentDate.AddDays(1);
        }

        int maxSessionIndex = existingSessions.Count;

        foreach (var cs in missingCourseSubjects)
        {
            subjectMap.TryGetValue(cs.SubjectId, out var subjectType);
            var currentSubject = allSubjects.FirstOrDefault(s => s.SubjectId == cs.SubjectId);
            TrainingType trainingType = TrainingTypeClassifier.Classify(currentSubject?.SubjectCode, currentSubject?.SubjectName, subjectType);

            // 1. Phân bổ địa điểm theo TrainingType và SubjectType (Facility / Location Routing)
            string sessionLocation;
            if (trainingType == TrainingType.Simulator)
            {
                sessionLocation = !string.IsNullOrWhiteSpace(classLocation)
                    ? (classLocation.Contains("Sim", StringComparison.OrdinalIgnoreCase) ? classLocation : $"{classLocation} (SIM Room)")
                    : "Buồng lái mô phỏng (SIM / FSTD Room)";
            }
            else if (trainingType == TrainingType.Flight)
            {
                sessionLocation = !string.IsNullOrWhiteSpace(classLocation)
                    ? (classLocation.Contains("Bay", StringComparison.OrdinalIgnoreCase) ||
                       classLocation.Contains("Airport", StringComparison.OrdinalIgnoreCase) ||
                       classLocation.Contains("Sân bay", StringComparison.OrdinalIgnoreCase) ||
                       classLocation.Contains("Flight", StringComparison.OrdinalIgnoreCase)
                        ? classLocation
                        : $"{classLocation} (Khu vực bay)")
                    : "Sân bay huấn luyện / Khu vực bay (Airfield)";
            }
            else
            {
                bool isWorkshopOrNonFstdPractical = (!string.IsNullOrEmpty(subjectType) && subjectType.Contains("Practical", StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(currentSubject?.SubjectName) && (
                        currentSubject.SubjectName.Contains("Maintenance", StringComparison.OrdinalIgnoreCase) ||
                        currentSubject.SubjectName.Contains("Bảo dưỡng", StringComparison.OrdinalIgnoreCase) ||
                        currentSubject.SubjectName.Contains("Cabin", StringComparison.OrdinalIgnoreCase) ||
                        currentSubject.SubjectName.Contains("Workshop", StringComparison.OrdinalIgnoreCase) ||
                        currentSubject.SubjectName.Contains("Xưởng", StringComparison.OrdinalIgnoreCase) ||
                        currentSubject.SubjectName.Contains("Thực hành", StringComparison.OrdinalIgnoreCase)
                    ));

                if (!string.IsNullOrWhiteSpace(classLocation))
                {
                    sessionLocation = classLocation;
                }
                else if (isWorkshopOrNonFstdPractical)
                {
                    sessionLocation = "Xưởng thực hành / Phòng huấn luyện an toàn (Ground Workshop)";
                }
                else
                {
                    sessionLocation = "Phòng học lý thuyết (Ground Classroom)";
                }
            }

            int sessionCount = cs.RequiredSessions > 0 ? cs.RequiredSessions : 1;

            // Deterministic sorting by DisplayOrder then Id
            var subjectAssessments = assessments
                .Where(a => a.SubjectId == cs.SubjectId)
                .OrderBy(a => a.DisplayOrder)
                .ThenBy(a => a.AssessmentId)
                .ToList();

            var subjectChecklists = checklists
                .Where(c => c.SubjectId == cs.SubjectId)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.PracticalChecklistId)
                .ToList();

            // Compatible assessment selection based on TrainingType:
            // - Theory sessions receive Theory assessments
            // - Flight/Simulator sessions receive PracticalChecklists and/or Practical assessments
            var subjectAssessment = trainingType == TrainingType.Theory
                ? (subjectAssessments.FirstOrDefault(a => string.Equals(a.AssessmentType, "Theory", StringComparison.OrdinalIgnoreCase)) ?? subjectAssessments.FirstOrDefault())
                : subjectAssessments.FirstOrDefault(a => string.Equals(a.AssessmentType, "Practical", StringComparison.OrdinalIgnoreCase));

            var subjectChecklist = subjectChecklists.FirstOrDefault();

            for (int i = 1; i <= sessionCount; i++)
            {
                DateTime sessionDate = currentDate <= cls.EndDate.Date ? currentDate : cls.EndDate.Date;

                // 2. Gán bài kiểm tra / đánh giá vào buổi học cuối của môn (Assessment Assignment)
                bool isFinalSession = (i == sessionCount);
                int? assessmentId = null;
                int? checklistId = null;
                bool isAssessmentRequired = false;
                bool isChecklistRequired = false;
                int sessionNumber = ++maxSessionIndex;
                string title = $"Buổi {sessionNumber}";

                if (isFinalSession && subjectAssessment != null)
                {
                    assessmentId = subjectAssessment.AssessmentId;
                    isAssessmentRequired = true;
                }

                if (isFinalSession && (subjectChecklist != null || trainingType == TrainingType.Simulator || trainingType == TrainingType.Flight))
                {
                    if (subjectChecklist != null) checklistId = subjectChecklist.PracticalChecklistId;
                    isChecklistRequired = true;
                }

                if (assessmentId.HasValue && checklistId.HasValue)
                {
                    title = $"Buổi {sessionNumber} (Đánh giá Lý thuyết & Thực hành)";
                }
                else if (assessmentId.HasValue)
                {
                    title = $"Buổi {sessionNumber} (Đánh giá: {subjectAssessment!.ComponentName})";
                }
                else if (isChecklistRequired)
                {
                    if (trainingType == TrainingType.Simulator)
                    {
                        title = $"Buổi {sessionNumber} (Đánh giá thực hành buồng lái mô phỏng)";
                    }
                    else if (trainingType == TrainingType.Flight)
                    {
                        title = $"Buổi {sessionNumber} (Đánh giá thực hành bay)";
                    }
                    else
                    {
                        title = $"Buổi {sessionNumber} (Đánh giá thực hành quy trình)";
                    }
                }

                string lessonPrefix = currentSubject?.SubjectCode ?? "SUB";
                string lessonCode = $"{lessonPrefix}-L{i:D2}";

                var session = new Session
                {
                    ClassId = cls.ClassId,
                    SubjectId = cs.SubjectId,
                    SessionTitle = title,
                    SessionDate = sessionDate,
                    Location = sessionLocation,
                    AssessmentId = assessmentId,
                    PracticalChecklistId = checklistId,
                    IsAssessmentRequired = isAssessmentRequired,
                    IsChecklistRequired = isChecklistRequired,
                    IsConfirmed = false,
                    TrainingType = trainingType,
                    LessonCode = lessonCode
                };

                await _unitOfWork.SessionRepository.AddAsync(session, ct);
                sessions.Add(session);


                // 3. Quản trị mệt mỏi ICAO (Fatigue Risk Management):
                // Môn SIM/Practical tối đa 4h/ngày -> mỗi ngày xếp 1 buổi SIM.
                // Môn Theory tối đa 8h/ngày -> mỗi ngày xếp 1 buổi lý thuyết.
                // Tăng dần ngày và bỏ qua Chủ nhật
                currentDate = currentDate.AddDays(1);
                if (currentDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    currentDate = currentDate.AddDays(1);
                }
            }
        }

        return sessions;
    }

    private static string? ResolveInstructorName(int? instructorAccountId, IEnumerable<UserProfile> profiles, IEnumerable<Account> accounts)
    {
        if (!instructorAccountId.HasValue) return null;
        var p = profiles.FirstOrDefault(x => x.AccountId == instructorAccountId.Value);
        var a = accounts.FirstOrDefault(x => x.AccountId == instructorAccountId.Value);
        return p?.FullName ?? a?.Username ?? $"Instructor #{instructorAccountId.Value}";
    }
}
