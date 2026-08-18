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

        return visible.Select(c => {
            var assignments = allClassSubjects
                .Where(cs => cs.ClassId == c.ClassId)
                .Select(cs => new InstructorAssignmentResponse(cs.ClassSubjectId, cs.SubjectId, cs.InstructorAccountId))
                .ToList();

            return new TrainingClassResponse(
                c.ClassId, c.ClassCode, c.ClassName, c.CourseId, c.StartDate, c.EndDate, c.Location, c.Capacity, c.Status, assignments);
        });
    }

    public async Task<TrainingClassResponse> GetClassByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var c = await _unitOfWork.ClassRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Class not found.");

        if (c.IsDeleted) throw new KeyNotFoundException("Class not found.");

        var allClassSubjects = await _unitOfWork.ClassSubjectRepository.GetAllAsync(cancellationToken);
        var assignments = allClassSubjects
            .Where(cs => cs.ClassId == c.ClassId)
            .Select(cs => new InstructorAssignmentResponse(cs.ClassSubjectId, cs.SubjectId, cs.InstructorAccountId))
            .ToList();

        return new TrainingClassResponse(c.ClassId, c.ClassCode, c.ClassName, c.CourseId, c.StartDate, c.EndDate, c.Location, c.Capacity, c.Status, assignments);
    }

    public async Task<TrainingClassResponse> CreateClassAsync(CreateClassRequest request, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var course = await _unitOfWork.CourseRepository.GetByIdAsync(request.CourseId, ct)
                    ?? throw new BusinessRuleViolationException("Course not found.");

                var cls = new Class
                {
                    ClassCode = request.ClassCode,
                    ClassName = request.ClassName,
                    CourseId = request.CourseId,
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

                // Lấy toàn bộ môn học thuộc khóa học (CourseSubjects)
                var courseSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(ct))
                    .Where(x => x.CourseId == request.CourseId).ToList();

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

                // 2. Tự động tạo sẵn danh sách Sessions (Buổi học) cho từng môn học
                foreach (var cs in courseSubjects)
                {
                    // Nếu RequiredSessions chưa set hoặc <= 0, mặc định tạo ít nhất 1 buổi
                    int sessionCount = cs.RequiredSessions > 0 ? cs.RequiredSessions : 1;

                    for (int i = 1; i <= sessionCount; i++)
                    {
                        var session = new Session
                        {
                            ClassId = cls.ClassId,
                            SubjectId = cs.SubjectId,
                            SessionTitle = $"Buổi {i}",
                            SessionDate = null, // Giảng viên sẽ lên lịch hoặc điểm danh sau
                            Location = request.Location,
                            IsConfirmed = false
                        };
                        await _unitOfWork.SessionRepository.AddAsync(session, ct);
                        sessions.Add(session);
                    }
                }
                await _unitOfWork.SaveAsync(ct);

                assignments = classSubjects.Select(x => new InstructorAssignmentResponse(x.ClassSubjectId, x.SubjectId, x.InstructorAccountId)).ToList();

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
                await _unitOfWork.CommitTransactionAsync(ct);

                return new TrainingClassResponse(cls.ClassId, cls.ClassCode, cls.ClassName, cls.CourseId, cls.StartDate, cls.EndDate, cls.Location, cls.Capacity, cls.Status, assignments);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
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
                foreach (var ea in existingAssignments)
                {
                    _unitOfWork.ClassSubjectRepository.Delete(ea);
                }
                await _unitOfWork.SaveAsync(ct); // Clear existing

                var assignments = new List<InstructorAssignmentResponse>();
                var classSubjects = new List<ClassSubject>();
                var courseSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(ct))
                    .Where(x => x.CourseId == request.CourseId).ToList();

                var assignmentDict = request.InstructorAssignments?
                    .Where(a => a.InstructorAccountId.HasValue)
                    .ToDictionary(a => a.SubjectId, a => a.InstructorAccountId) ?? new Dictionary<int, int?>();

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

                // 2. Kiểm tra nếu lớp này chưa có Session nào thì tự động sinh Sessions
                var existingSessions = (await _unitOfWork.SessionRepository.GetAllAsync(ct))
                    .Where(s => s.ClassId == cls.ClassId && !s.IsDeleted).ToList();

                if (!existingSessions.Any())
                {
                    foreach (var cs in courseSubjects)
                    {
                        int sessionCount = cs.RequiredSessions > 0 ? cs.RequiredSessions : 1;
                        for (int i = 1; i <= sessionCount; i++)
                        {
                            var session = new Session
                            {
                                ClassId = cls.ClassId,
                                SubjectId = cs.SubjectId,
                                SessionTitle = $"Buổi {i}",
                                SessionDate = null,
                                Location = request.Location,
                                IsConfirmed = false
                            };
                            await _unitOfWork.SessionRepository.AddAsync(session, ct);
                        }
                    }
                    await _unitOfWork.SaveAsync(ct);
                }

                assignments = classSubjects.Select(x => new InstructorAssignmentResponse(x.ClassSubjectId, x.SubjectId, x.InstructorAccountId)).ToList();

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

                return new TrainingClassResponse(cls.ClassId, cls.ClassCode, cls.ClassName, cls.CourseId, cls.StartDate, cls.EndDate, cls.Location, cls.Capacity, cls.Status, assignments);
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
}
