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
        var allFacilities = _unitOfWork.TrainingFacilityRepository != null
            ? await _unitOfWork.TrainingFacilityRepository.GetAllAsync(cancellationToken)
            : (IReadOnlyList<TrainingFacility>)Array.Empty<TrainingFacility>();

        return visible.Select(c => {
            var assignments = allClassSubjects
                .Where(cs => cs.ClassId == c.ClassId)
                .Select(cs => new InstructorAssignmentResponse(
                    cs.ClassSubjectId, 
                    cs.SubjectId, 
                    cs.InstructorAccountId,
                    ResolveInstructorName(cs.InstructorAccountId, allProfiles, allAccounts)))
                .ToList();

            var defFac = allFacilities.FirstOrDefault(f => f.FacilityId == c.DefaultFacilityId);

            return new TrainingClassResponse(
                c.ClassId, c.ClassCode, c.ClassName, c.CourseId, c.StartDate, c.EndDate, c.Location, c.Capacity, c.Status, assignments, c.CourseVersionNo, c.DefaultFacilityId, defFac?.FacilityName);
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
        var defFac = (c.DefaultFacilityId.HasValue && _unitOfWork.TrainingFacilityRepository != null)
            ? await _unitOfWork.TrainingFacilityRepository.GetByIdAsync(c.DefaultFacilityId.Value, cancellationToken)
            : null;

        var assignments = allClassSubjects
            .Where(cs => cs.ClassId == c.ClassId)
            .Select(cs => new InstructorAssignmentResponse(
                cs.ClassSubjectId, 
                cs.SubjectId, 
                cs.InstructorAccountId,
                ResolveInstructorName(cs.InstructorAccountId, allProfiles, allAccounts)))
            .ToList();

        return new TrainingClassResponse(c.ClassId, c.ClassCode, c.ClassName, c.CourseId, c.StartDate, c.EndDate, c.Location, c.Capacity, c.Status, assignments, c.CourseVersionNo, c.DefaultFacilityId, defFac?.FacilityName);
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

        if (request.Status == ClassStatus.Completed)
        {
            throw new BusinessRuleViolationException("Không thể tạo mới lớp học với trạng thái 'Completed'. Lớp học mới chỉ có thể bắt đầu với trạng thái Planned hoặc InProgress.");
        }

        if (request.CourseId <= 0)
        {
            throw new BusinessRuleViolationException("Khóa học không hợp lệ. Vui lòng chọn một khóa học cụ thể.");
        }

        var course = await _unitOfWork.CourseRepository.GetByIdAsync(request.CourseId, ct);
        if (course == null || course.IsDeleted)
        {
            throw new BusinessRuleViolationException($"Khóa học (ID: #{request.CourseId}) không tồn tại trong hệ thống hoặc đã bị xóa. Vui lòng chọn một khóa học hợp lệ đang hoạt động.");
        }

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

        TrainingFacility? defaultFacility = null;
        if (request.DefaultFacilityId.HasValue)
        {
            defaultFacility = await _unitOfWork.TrainingFacilityRepository.GetByIdAsync(request.DefaultFacilityId.Value, ct);
            if (defaultFacility == null || defaultFacility.IsDeleted || !defaultFacility.IsActive)
            {
                throw new BusinessRuleViolationException($"Cơ sở đào tạo mặc định với ID {request.DefaultFacilityId.Value} không tồn tại hoặc đã ngừng hoạt động.");
            }
        }

        var cls = new Class
        {
            ClassCode = request.ClassCode,
            ClassName = request.ClassName,
            CourseId = request.CourseId,
            CourseVersionNo = course.VersionNo,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DefaultFacilityId = request.DefaultFacilityId,
            Location = defaultFacility?.FacilityName ?? request.Location,
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

        var allProfiles = _unitOfWork.UserProfileRepository != null ? (await _unitOfWork.UserProfileRepository.GetAllIncludingDeletedAsync(ct) ?? new List<UserProfile>()) : new List<UserProfile>();
        var allAccounts = _unitOfWork.AccountRepository != null ? (await _unitOfWork.AccountRepository.GetAllIncludingDeletedAsync(ct) ?? new List<Account>()) : new List<Account>();
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

        return new TrainingClassResponse(cls.ClassId, cls.ClassCode, cls.ClassName, cls.CourseId, cls.StartDate, cls.EndDate, cls.Location, cls.Capacity, cls.Status, assignments, cls.CourseVersionNo, cls.DefaultFacilityId, defaultFacility?.FacilityName);
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

                if (oldStatus == ClassStatus.Completed && request.Status != ClassStatus.Completed)
                {
                    throw new BusinessRuleViolationException("Không thể thay đổi trạng thái của lớp học đã hoàn thành (Completed).");
                }

                if (request.Status == ClassStatus.Completed && oldStatus != ClassStatus.Completed)
                {
                    throw new BusinessRuleViolationException("Không thể chuyển trạng thái lớp sang 'Completed' thủ công. Lớp học sẽ được hệ thống tự động hoàn thành sau khi tất cả các môn học và buổi học kết thúc.");
                }

                // Aviation Safety Enforcement:
                // Khi bắt đầu lớp học (chuyển sang InProgress), TẤT CẢ học viên đang ghi danh (Active)
                // trong lớp này bắt buộc phải có hồ sơ năng định hợp lệ (đã được Academic duyệt, không bị Grounded, không quá hạn).
                if (request.Status == ClassStatus.InProgress && oldStatus != ClassStatus.InProgress)
                {
                    var activeEnrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(ct))
                        .Where(e => e.ClassId == id && e.Status == EnrollmentStatus.Active && !e.IsDeleted)
                        .ToList();

                    if (activeEnrollments.Count > 0)
                    {
                        var studentAccountIds = activeEnrollments.Select(e => e.AccountId).Distinct().ToList();

                        // Luôn truy vấn trực tiếp từ database để đảm bảo dữ liệu mới nhất ngay khi Academic vừa duyệt ở tab khác
                        var studentProfiles = (await _unitOfWork.UserProfileRepository.GetAllAsync(ct))
                            .Where(p => studentAccountIds.Contains(p.AccountId) && !p.IsDeleted)
                            .ToList();

                        var unqualifiedLearners = new List<string>();

                        foreach (var enrollment in activeEnrollments)
                        {
                            var profile = studentProfiles.FirstOrDefault(p => p.AccountId == enrollment.AccountId);
                            if (profile == null)
                            {
                                unqualifiedLearners.Add($"Học viên #{enrollment.AccountId} (chưa có hồ sơ cá nhân hoàn chỉnh)");
                                continue;
                            }

                            var issues = new List<string>();
                            if (!profile.IsCredentialsVerified)
                            {
                                issues.Add("chưa được duyệt hồ sơ năng định");
                            }
                            if (profile.Status == LearnerStatus.Grounded)
                            {
                                issues.Add("đang bị đình chỉ bay (Grounded)");
                            }
                            if (profile.LicenseExpiryDate.HasValue && profile.LicenseExpiryDate.Value.Date < DateTime.UtcNow.Date)
                            {
                                issues.Add($"bằng lái đã hết hạn ({profile.LicenseExpiryDate.Value:dd/MM/yyyy})");
                            }
                            if (profile.MedicalExpiryDate.HasValue && profile.MedicalExpiryDate.Value.Date < DateTime.UtcNow.Date)
                            {
                                issues.Add($"giấy KSK đã hết hạn ({profile.MedicalExpiryDate.Value:dd/MM/yyyy})");
                            }

                            if (issues.Count > 0)
                            {
                                unqualifiedLearners.Add($"{profile.FullName} ({profile.UserCode}): {string.Join(", ", issues)}");
                            }
                        }

                        if (unqualifiedLearners.Count > 0)
                        {
                            throw new BusinessRuleViolationException(
                                $"Không thể bắt đầu lớp học '{cls.ClassName}'. Có {unqualifiedLearners.Count} học viên chưa đủ điều kiện năng định:\n- " +
                                string.Join("\n- ", unqualifiedLearners) +
                                "\nVui lòng hoàn tất thẩm định tại mục 'Hồ sơ năng định' hoặc loại học viên chưa đạt khỏi danh sách lớp trước khi bắt đầu.");
                        }
                    }
                }

                if (request.CourseId > 0 && request.CourseId != cls.CourseId)
                {
                    var targetCourse = await _unitOfWork.CourseRepository.GetByIdAsync(request.CourseId, ct);
                    if (targetCourse == null || targetCourse.IsDeleted)
                    {
                        throw new BusinessRuleViolationException($"Không thể chuyển lớp học sang khóa học (ID: #{request.CourseId}) vì khóa học này không tồn tại hoặc đã bị xóa.");
                    }

                    var hasEnrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(ct))
                        .Any(e => e.ClassId == id && !e.IsDeleted);
                    if (hasEnrollments)
                    {
                        throw new BusinessRuleViolationException("Không thể thay đổi khóa học của lớp học đã có học viên ghi danh.");
                    }

                    cls.CourseId = request.CourseId;
                    cls.CourseVersionNo = targetCourse.VersionNo;
                }

                TrainingFacility? defaultFacility = null;
                if (request.DefaultFacilityId.HasValue)
                {
                    defaultFacility = await _unitOfWork.TrainingFacilityRepository.GetByIdAsync(request.DefaultFacilityId.Value, ct);
                    if (defaultFacility == null || defaultFacility.IsDeleted || !defaultFacility.IsActive)
                    {
                        throw new BusinessRuleViolationException($"Cơ sở đào tạo mặc định với ID {request.DefaultFacilityId.Value} không tồn tại hoặc đã ngừng hoạt động.");
                    }
                    cls.DefaultFacilityId = request.DefaultFacilityId.Value;
                    cls.Location = defaultFacility.FacilityName;
                }
                else
                {
                    cls.DefaultFacilityId = null;
                    if (request.Location != null) cls.Location = request.Location;
                }

                cls.ClassCode = request.ClassCode;
                cls.ClassName = request.ClassName;
                cls.StartDate = request.StartDate;
                cls.EndDate = request.EndDate;
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
                    bool hasEnrolledStudents = allEnrollments.Any(e => e.ClassId == cls.ClassId && !e.IsDeleted && e.Status != EnrollmentStatus.Withdrawn && e.Status != EnrollmentStatus.Deleted);

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

                return new TrainingClassResponse(cls.ClassId, cls.ClassCode, cls.ClassName, cls.CourseId, cls.StartDate, cls.EndDate, cls.Location, cls.Capacity, cls.Status, assignments, cls.CourseVersionNo, cls.DefaultFacilityId, defaultFacility?.FacilityName);
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

        var allSubjects = (await _unitOfWork.SubjectRepository.GetAllAsync(ct))?.ToList() ?? new List<Subject>();
        var allFacilities = _unitOfWork.TrainingFacilityRepository != null
            ? (await _unitOfWork.TrainingFacilityRepository.GetAllAsync(ct))?
                .Where(f => !f.IsDeleted && f.IsActive)
                .ToList() ?? new List<TrainingFacility>()
            : new List<TrainingFacility>();

        // Tải danh sách Assessments và PracticalChecklists thuộc khóa học để tự động gắn vào các buổi kiểm tra
        var assessments = (await _unitOfWork.AssessmentRepository.GetAllAsync(ct))?
            .Where(a => a.CourseId == cls.CourseId && !a.IsDeleted)
            .ToList() ?? new List<Assessment>();

        var checklists = (await _unitOfWork.PracticalChecklistRepository.GetAllAsync(ct))?
            .Where(pc => (pc.CourseId == cls.CourseId || pc.CourseId == 0) && !pc.IsDeleted)
            .ToList() ?? new List<PracticalChecklist>();

        var allSystemSessions = (await _unitOfWork.SessionRepository.GetAllAsync(ct))?
            .Where(s => !s.IsDeleted && s.SessionDate.HasValue)
            .ToList() ?? new List<Session>();
        var scheduledSessionsPool = new List<Session>(allSystemSessions);

        var existingSessions = allSystemSessions
            .Where(s => s.ClassId == cls.ClassId).ToList();
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

            bool isWorkshopOrNonFstdPractical = FacilityCompatibilityHelper.IsWorkshopOrPracticalSubject(
                currentSubject?.SubjectCode, currentSubject?.SubjectName, subjectType);
            var compatTypes = FacilityCompatibilityHelper.GetCompatibleFacilityTypes(trainingType, isWorkshopOrNonFstdPractical);

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
                // 1. Phân bổ ca học thông minh & Gán cơ sở vật chất (Smart Time Slot & Facility Routing)
                DateTime candidateDate = currentDate.Date;
                DateTime allocatedStart = candidateDate.AddHours(8);
                DateTime allocatedEnd = allocatedStart.AddHours(2);
                TrainingFacility? assignedFacility = null;
                bool allocated = false;

                // Các ca học tiêu chuẩn (UTC: 00:30, 02:45, 06:30, 08:45 tương đương 07:30, 09:45, 13:30, 15:45 GMT+7)
                var standardSlots = new[]
                {
                    new TimeSpan(0, 30, 0),
                    new TimeSpan(2, 45, 0),
                    new TimeSpan(6, 30, 0),
                    new TimeSpan(8, 45, 0)
                };

                while (!allocated)
                {
                    if (candidateDate.DayOfWeek == DayOfWeek.Sunday)
                    {
                        candidateDate = candidateDate.AddDays(1);
                        continue;
                    }

                    foreach (var slot in standardSlots)
                    {
                        var slotStart = candidateDate.Add(slot);
                        var slotEnd = slotStart.AddHours(2);

                        // Kiểm tra xung đột với lớp này
                        bool classConflict = scheduledSessionsPool.Any(s =>
                            s.ClassId == cls.ClassId &&
                            FacilityCompatibilityHelper.HasTimeOverlap(
                                slotStart, slotEnd,
                                s.StartAt ?? s.SessionDate ?? DateTime.MinValue,
                                s.EndAt ?? (s.SessionDate.HasValue ? s.SessionDate.Value.AddHours(2) : DateTime.MinValue)));

                        if (classConflict) continue;

                        // Tìm cơ sở đào tạo phù hợp
                        TrainingFacility? candidateFacility = null;
                        if (cls.DefaultFacilityId.HasValue)
                        {
                            var defFac = allFacilities.FirstOrDefault(f => f.FacilityId == cls.DefaultFacilityId.Value);
                            if (defFac != null && compatTypes.Contains(defFac.FacilityType))
                            {
                                bool facConflict = scheduledSessionsPool.Any(s =>
                                    s.FacilityId == defFac.FacilityId &&
                                    FacilityCompatibilityHelper.HasTimeOverlap(
                                        slotStart, slotEnd,
                                        s.StartAt ?? s.SessionDate ?? DateTime.MinValue,
                                        s.EndAt ?? (s.SessionDate.HasValue ? s.SessionDate.Value.AddHours(2) : DateTime.MinValue)));

                                if (!facConflict) candidateFacility = defFac;
                            }
                        }

                        if (candidateFacility == null)
                        {
                            foreach (var fac in allFacilities.Where(f => compatTypes.Contains(f.FacilityType)))
                            {
                                bool facConflict = scheduledSessionsPool.Any(s =>
                                    s.FacilityId == fac.FacilityId &&
                                    FacilityCompatibilityHelper.HasTimeOverlap(
                                        slotStart, slotEnd,
                                        s.StartAt ?? s.SessionDate ?? DateTime.MinValue,
                                        s.EndAt ?? (s.SessionDate.HasValue ? s.SessionDate.Value.AddHours(2) : DateTime.MinValue)));

                                if (!facConflict)
                                {
                                    candidateFacility = fac;
                                    break;
                                }
                            }
                        }

                        // Nếu tìm được slot và không xung đột lớp (có thể có hoặc chưa có facility nếu tất cả phòng đầy)
                        allocatedStart = slotStart;
                        allocatedEnd = slotEnd;
                        assignedFacility = candidateFacility;
                        allocated = true;
                        break;
                    }

                    if (!allocated)
                    {
                        candidateDate = candidateDate.AddDays(1);
                    }
                }

                DateTime sessionStart = allocatedStart;
                DateTime sessionEnd = allocatedEnd;
                int? facilityId = assignedFacility?.FacilityId;
                string? sessionLocation = assignedFacility != null 
                    ? assignedFacility.FacilityName 
                    : (!string.IsNullOrWhiteSpace(classLocation) 
                        ? classLocation 
                        : (trainingType == TrainingType.Simulator
                            ? "Buồng lái mô phỏng (SIM / FSTD Room)"
                            : trainingType == TrainingType.Flight
                                ? "Sân bay huấn luyện / Khu vực bay (Airfield)"
                                : isWorkshopOrNonFstdPractical
                                    ? "Xưởng thực hành / Phòng huấn luyện an toàn (Ground Workshop)"
                                    : "Phòng học lý thuyết (Ground Classroom)"));

                // 2. Gán bài kiểm tra / đánh giá vào buổi học cuối của môn (Assessment Assignment)
                bool isFinalSession = (i == sessionCount);
                int? assessmentId = null;
                int? checklistId = null;
                bool isAssessmentRequired = false;
                bool isChecklistRequired = false;
                string title = $"Buổi {i}";

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
                    title = $"Buổi {i} (Đánh giá Lý thuyết & Thực hành)";
                }
                else if (assessmentId.HasValue)
                {
                    title = $"Buổi {i} (Đánh giá: {subjectAssessment!.ComponentName})";
                }
                else if (isChecklistRequired)
                {
                    if (trainingType == TrainingType.Simulator)
                    {
                        title = $"Buổi {i} (Đánh giá thực hành buồng lái mô phỏng)";
                    }
                    else if (trainingType == TrainingType.Flight)
                    {
                        title = $"Buổi {i} (Đánh giá thực hành bay)";
                    }
                    else
                    {
                        title = $"Buổi {i} (Đánh giá thực hành quy trình)";
                    }
                }

                string lessonPrefix = currentSubject?.SubjectCode ?? "SUB";
                string lessonCode = $"{lessonPrefix}-L{i:D2}";

                var session = new Session
                {
                    ClassId = cls.ClassId,
                    SubjectId = cs.SubjectId,
                    SessionTitle = title,
                    SessionDate = sessionStart,
                    StartAt = sessionStart,
                    EndAt = sessionEnd,
                    FacilityId = facilityId,
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
                scheduledSessionsPool.Add(session);

                // 3. Quản trị mệt mỏi ICAO (Fatigue Risk Management):
                // Tăng dần ngày và bỏ qua Chủ nhật
                currentDate = candidateDate.AddDays(1);
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
