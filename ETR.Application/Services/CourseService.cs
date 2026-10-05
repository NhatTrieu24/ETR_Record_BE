using ETR.Application.Compliance;
using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using ETR.Domain.Enums;

namespace ETR.Application.Services;

public class CourseService : ICourseService
{
    private readonly IUnitOfWork _unitOfWork;

    public CourseService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task EnsureCourseNotLockedAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var course = await _unitOfWork.CourseRepository.GetByIdAsync(courseId, cancellationToken);
        if (course == null || course.IsDeleted) return;

        var allClasses = await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken);
        var classes = (allClasses ?? Enumerable.Empty<Class>())
            .Where(c => c.CourseId == courseId && !c.IsDeleted).ToList();

        var hasLockedClasses = classes.Any(c =>
            c.Status == ClassStatus.Scheduled ||
            c.Status == ClassStatus.InProgress ||
            c.Status == ClassStatus.Completed);

        if (hasLockedClasses)
        {
            throw new BusinessRuleViolationException(
                $"Giáo trình của khóa học '{course.CourseName}' (Mã: {course.CourseCode}, Version: {course.VersionNo}) đã được mở lớp đào tạo chính thức (Scheduled/InProgress/Completed) nên đã bị đóng băng bất biến. Để sửa đổi giáo trình, vui lòng tạo phiên bản mới (New Version).");
        }

        if (classes.Any())
        {
            var classIds = classes.Select(c => c.ClassId).ToHashSet();
            var allEnrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken);
            var hasEnrollments = (allEnrollments ?? Enumerable.Empty<CourseEnrollment>())
                .Any(e => classIds.Contains(e.ClassId) && !e.IsDeleted);

            if (hasEnrollments)
            {
                throw new BusinessRuleViolationException(
                    $"Giáo trình của khóa học '{course.CourseName}' (Mã: {course.CourseCode}, Version: {course.VersionNo}) đã có học viên ghi danh nên đã bị đóng băng bất biến. Để sửa đổi giáo trình, vui lòng tạo phiên bản mới (New Version).");
            }
        }
    }

    private async Task<(List<CourseDepartment> courseDepartments, Dictionary<int, string> departments)> SafeGetCourseDepartmentsAsync(CancellationToken cancellationToken)
    {
        var courseDepartments = new List<CourseDepartment>();
        var departments = new Dictionary<int, string>();

        try
        {
            if (_unitOfWork.CourseDepartmentRepository != null)
            {
                var cds = await _unitOfWork.CourseDepartmentRepository.GetAllAsync(cancellationToken);
                if (cds != null) courseDepartments = cds.Where(cd => !cd.IsDeleted).ToList();
            }
        }
        catch
        {
            // Fallback gracefully if CourseDepartments table has not been created yet by migration
        }

        try
        {
            if (_unitOfWork.DepartmentRepository != null)
            {
                var depts = await _unitOfWork.DepartmentRepository.GetAllAsync(cancellationToken);
                if (depts != null) departments = depts.ToDictionary(d => d.DepartmentId, d => d.DepartmentName);
            }
        }
        catch
        {
            // Fallback gracefully if DepartmentCode/IsTrainingAudience columns have not been added yet by migration
        }

        return (courseDepartments, departments);
    }

    public async Task<IEnumerable<CourseResponse>> GetAllCoursesAsync(CancellationToken cancellationToken = default)
    {
        var courses = await _unitOfWork.CourseRepository.GetAllAsync(cancellationToken);
        var (courseDepartments, departments) = await SafeGetCourseDepartmentsAsync(cancellationToken);

        return courses.Where(c => !c.IsDeleted).Select(c =>
        {
            var cdList = courseDepartments.Where(cd => cd.CourseId == c.CourseId).ToList();
            var deptIds = cdList.Select(cd => cd.DepartmentId).ToList();
            var deptNames = deptIds.Where(id => departments.ContainsKey(id)).Select(id => departments[id]).ToList();

            return new CourseResponse(
                c.CourseId, c.CourseCode, c.CourseName, c.Description, c.DurationHours, c.Status, c.ValidityMonths, c.CourseType,
                VersionNo: c.VersionNo, PreviousVersionId: c.PreviousVersionId,
                DepartmentIds: deptIds.Count > 0 ? deptIds : null,
                DepartmentNames: deptNames.Count > 0 ? deptNames : null);
        });
    }

    public async Task<CourseResponse> GetCourseByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var c = await _unitOfWork.CourseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Course not found.");

        if (c.IsDeleted) throw new KeyNotFoundException("Course not found.");

        var subjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(cancellationToken))
            .Where(cs => cs.CourseId == id && !cs.IsDeleted)
            .OrderBy(cs => cs.SequenceNo)
            .Select(cs => new CourseSubjectResponse(
                cs.CourseId, cs.SubjectId, cs.SequenceNo, cs.RequiredHours, cs.RequiredSessions, cs.IsMandatory, cs.PassingScore, cs.SubjectVersion
            )).ToList();

        var (courseDepartments, departments) = await SafeGetCourseDepartmentsAsync(cancellationToken);
        var cdList = courseDepartments.Where(cd => cd.CourseId == id).ToList();
        var deptIds = cdList.Select(cd => cd.DepartmentId).ToList();
        var deptNames = deptIds.Where(deptId => departments.ContainsKey(deptId)).Select(deptId => departments[deptId]).ToList();

        return new CourseResponse(
            c.CourseId, c.CourseCode, c.CourseName, c.Description, c.DurationHours, c.Status, c.ValidityMonths, c.CourseType,
            subjects, c.VersionNo, c.PreviousVersionId,
            DepartmentIds: deptIds.Count > 0 ? deptIds : null,
            DepartmentNames: deptNames.Count > 0 ? deptNames : null);
    }

    public async Task<CourseResponse> CreateCourseAsync(CreateCourseRequest request, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        if (request.Subjects == null || !request.Subjects.Any())
        {
            throw new ArgumentException("A course must have at least one subject configured upon creation.");
        }

        var isDuplicate = _unitOfWork.CourseRepository.GetQueryable()
            .Any(c => c.CourseCode == request.CourseCode && c.VersionNo == 1 && !c.IsDeleted);
        if (isDuplicate)
        {
            throw new BusinessRuleViolationException($"A course with code '{request.CourseCode}' (version 1) already exists.");
        }

        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var course = new Course
                {
                    CourseCode = request.CourseCode,
                    CourseName = request.CourseName,
                    Description = request.Description,
                    DurationHours = request.DurationHours,
                    Status = request.Status,
                    ValidityMonths = request.ValidityMonths,
                    CourseType = request.CourseType,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByAccountId = createdByAccountId
                };

                await _unitOfWork.CourseRepository.AddAsync(course, ct);
                await _unitOfWork.SaveAsync(ct);

                await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                {
                    AccountId = createdByAccountId,
                    ActionType = AuditActionType.INSERT.ToString(),
                    EntityName = nameof(Course),
                    RecordId = course.CourseId,
                    NewValue = course.CourseCode,
                    Description = $"Course #{course.CourseId} ({course.CourseCode}) created"
                }, ct);

                var responseSubjects = new List<CourseSubjectResponse>();
                foreach (var s in request.Subjects)
                {
                    var subject = await _unitOfWork.SubjectRepository.GetByIdAsync(s.SubjectId, ct)
                        ?? throw new KeyNotFoundException($"Subject {s.SubjectId} not found.");

                    var courseSubject = new CourseSubject
                    {
                        CourseId = course.CourseId,
                        SubjectId = s.SubjectId,
                        SequenceNo = s.SequenceNo,
                        RequiredHours = s.RequiredHours,
                        RequiredSessions = s.RequiredSessions,
                        IsMandatory = s.IsMandatory,
                        PassingScore = s.PassingScore,
                        SubjectVersion = s.SubjectVersion,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByAccountId = createdByAccountId
                    };

                    await _unitOfWork.CourseSubjectRepository.AddAsync(courseSubject, ct);

                    await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                    {
                        AccountId = createdByAccountId,
                        ActionType = AuditActionType.INSERT.ToString(),
                        EntityName = nameof(CourseSubject),
                        RecordId = course.CourseId,
                        NewValue = $"SubjectId: {s.SubjectId}, Seq: {s.SequenceNo}",
                        Description = $"Assigned Subject #{s.SubjectId} to new Course #{course.CourseId}"
                    }, ct);

                    responseSubjects.Add(new CourseSubjectResponse(
                        course.CourseId, s.SubjectId, s.SequenceNo, s.RequiredHours, s.RequiredSessions, s.IsMandatory, s.PassingScore, s.SubjectVersion
                    ));
                }

                var deptIdsList = new List<int>();
                var deptNamesList = new List<string>();
                if (request.DepartmentIds != null && request.DepartmentIds.Any() && _unitOfWork.DepartmentRepository != null && _unitOfWork.CourseDepartmentRepository != null)
                {
                    var validatedDepts = await ValidateAndGetTrainingAudienceDepartmentsAsync(request.DepartmentIds, ct);

                    foreach (var dept in validatedDepts)
                    {
                        var courseDept = new CourseDepartment
                        {
                            CourseId = course.CourseId,
                            DepartmentId = dept.DepartmentId,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByAccountId = createdByAccountId
                        };
                        await _unitOfWork.CourseDepartmentRepository.AddAsync(courseDept, ct);
                        deptIdsList.Add(dept.DepartmentId);
                        deptNamesList.Add(dept.DepartmentName);
                    }
                }

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return new CourseResponse(
                    course.CourseId,
                    course.CourseCode,
                    course.CourseName,
                    course.Description,
                    course.DurationHours,
                    course.Status,
                    course.ValidityMonths,
                    course.CourseType,
                    responseSubjects,
                    course.VersionNo,
                    DepartmentIds: deptIdsList.Count > 0 ? deptIdsList : null,
                    DepartmentNames: deptNamesList.Count > 0 ? deptNamesList : null);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    public async Task<CourseResponse> UpdateCourseAsync(int id, UpdateCourseRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        if (request.Subjects == null || !request.Subjects.Any())
        {
            throw new ArgumentException("A course must have at least one subject.");
        }

        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var course = await _unitOfWork.CourseRepository.GetByIdAsync(id, ct)
                    ?? throw new KeyNotFoundException("Course not found.");

                if (course.IsDeleted) throw new KeyNotFoundException("Course not found.");

                var existingSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(ct))
                    .Where(cs => cs.CourseId == id && !cs.IsDeleted).ToList();

                // If course is locked, only status transition (e.g. Draft -> Active, Active -> Archived) without modifying curriculum content or subjects is allowed
                bool isCurriculumUnchanged = course.CourseCode == request.CourseCode
                    && course.CourseName == request.CourseName
                    && course.Description == request.Description
                    && course.DurationHours == request.DurationHours
                    && course.ValidityMonths == request.ValidityMonths
                    && course.CourseType == request.CourseType;

                bool areSubjectsUnchanged = existingSubjects.Count == request.Subjects.Count
                    && request.Subjects.All(reqSub =>
                    {
                        var match = existingSubjects.FirstOrDefault(e => e.SubjectId == reqSub.SubjectId);
                        return match != null
                            && match.SequenceNo == reqSub.SequenceNo
                            && match.RequiredHours == reqSub.RequiredHours
                            && match.RequiredSessions == reqSub.RequiredSessions
                            && match.IsMandatory == reqSub.IsMandatory
                            && match.PassingScore == reqSub.PassingScore
                            && match.SubjectVersion == reqSub.SubjectVersion;
                    });

                bool isOnlyStatusChange = isCurriculumUnchanged && areSubjectsUnchanged;

                if (!isOnlyStatusChange)
                {
                    await EnsureCourseNotLockedAsync(id, ct);
                }

                var isDuplicate = _unitOfWork.CourseRepository.GetQueryable()
                    .Any(c => c.CourseId != id && c.CourseCode == request.CourseCode && c.VersionNo == course.VersionNo && !c.IsDeleted);
                if (isDuplicate)
                {
                    throw new BusinessRuleViolationException($"A course with code '{request.CourseCode}' and version {course.VersionNo} already exists.");
                }

                var oldStatus = course.Status;

                course.CourseCode = request.CourseCode;
                course.CourseName = request.CourseName;
                course.Description = request.Description;
                course.DurationHours = request.DurationHours;
                course.Status = request.Status;
                course.ValidityMonths = request.ValidityMonths;
                course.CourseType = request.CourseType;
                course.UpdatedAt = DateTime.UtcNow;
                course.UpdatedByAccountId = updatedByAccountId;

                _unitOfWork.CourseRepository.Update(course);

                await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                {
                    AccountId = updatedByAccountId,
                    ActionType = AuditActionType.UPDATE.ToString(),
                    EntityName = nameof(Course),
                    RecordId = course.CourseId,
                    OldValue = oldStatus.ToString(),
                    NewValue = course.Status.ToString(),
                    Description = $"Course #{course.CourseId} ({course.CourseCode} v{course.VersionNo}) updated"
                }, ct);

                // SYNC SUBJECTS
                var requestedSubjectIds = request.Subjects.Select(s => s.SubjectId).ToList();

                // 1. Validate and remove subjects not in the request
                var subjectsToRemove = existingSubjects.Where(cs => !requestedSubjectIds.Contains(cs.SubjectId)).ToList();
                if (subjectsToRemove.Any())
                {
                    var nonCancelledClasses = (await _unitOfWork.ClassRepository.GetAllAsync(ct))
                        .Where(c => c.CourseId == id && !c.IsDeleted && c.Status != ClassStatus.Cancelled)
                        .ToList();

                    if (nonCancelledClasses.Any())
                    {
                        var classIds = nonCancelledClasses.Select(c => c.ClassId).ToHashSet();
                        var toRemoveIds = subjectsToRemove.Select(s => s.SubjectId).ToHashSet();

                        var conflictingClassSubjects = (await _unitOfWork.ClassSubjectRepository.GetAllAsync(ct))
                            .Where(cs => classIds.Contains(cs.ClassId) && toRemoveIds.Contains(cs.SubjectId) && !cs.IsDeleted)
                            .ToList();

                        if (conflictingClassSubjects.Any())
                        {
                            var conflictSubjectIds = conflictingClassSubjects.Select(cs => cs.SubjectId).Distinct().ToList();
                            var conflictClassIds = conflictingClassSubjects.Select(cs => cs.ClassId).ToHashSet();
                            var ongoingClassCodes = nonCancelledClasses
                                .Where(c => conflictClassIds.Contains(c.ClassId) && (c.Status == ClassStatus.InProgress || c.Status == ClassStatus.Scheduled))
                                .Select(c => c.ClassCode)
                                .Distinct()
                                .ToList();

                            if (ongoingClassCodes.Any())
                            {
                                throw new BusinessRuleViolationException(
                                    $"Không thể gỡ các môn học [ID: {string.Join(", ", conflictSubjectIds)}] khỏi khóa học vì đang có lớp học chưa kết thúc ({string.Join(", ", ongoingClassCodes)}) đang giảng dạy môn học này.");
                            }

                            throw new BusinessRuleViolationException(
                                $"Không thể gỡ các môn học [ID: {string.Join(", ", conflictSubjectIds)}] khỏi khóa học vì đã có lớp học thuộc khóa được thiết lập môn học này.");
                        }
                    }

                    foreach (var toRemove in subjectsToRemove)
                    {
                        toRemove.IsDeleted = true;
                        toRemove.DeletedAt = DateTime.UtcNow;
                        _unitOfWork.CourseSubjectRepository.Update(toRemove);

                        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                        {
                            AccountId = updatedByAccountId,
                            ActionType = AuditActionType.DELETE.ToString(),
                            EntityName = nameof(CourseSubject),
                            RecordId = course.CourseId,
                            OldValue = $"SubjectId: {toRemove.SubjectId}",
                            NewValue = "Deleted",
                            Description = $"Removed Subject #{toRemove.SubjectId} from Course #{course.CourseId} during full sync"
                        }, ct);
                    }
                }

                // 2. Add or Update subjects
                var ongoingClasses = (await _unitOfWork.ClassRepository.GetAllAsync(ct))
                    .Where(c => c.CourseId == id && !c.IsDeleted && (c.Status == ClassStatus.InProgress || c.Status == ClassStatus.Scheduled))
                    .ToList();
                var ongoingClassIds = ongoingClasses.Select(c => c.ClassId).ToHashSet();
                var ongoingClassSubjects = ongoingClassIds.Any()
                    ? (await _unitOfWork.ClassSubjectRepository.GetAllAsync(ct))
                        .Where(cs => ongoingClassIds.Contains(cs.ClassId) && !cs.IsDeleted)
                        .ToList()
                    : new List<ClassSubject>();

                var finalSubjects = new List<CourseSubjectResponse>();
                foreach (var reqSub in request.Subjects)
                {
                    var existing = existingSubjects.FirstOrDefault(cs => cs.SubjectId == reqSub.SubjectId);
                    if (existing != null)
                    {
                        bool isChangingCriteria = existing.PassingScore != reqSub.PassingScore ||
                                                  existing.RequiredHours != reqSub.RequiredHours ||
                                                  existing.RequiredSessions != reqSub.RequiredSessions ||
                                                  existing.IsMandatory != reqSub.IsMandatory;

                        if (isChangingCriteria && ongoingClassSubjects.Any(cs => cs.SubjectId == existing.SubjectId))
                        {
                            var affectedClasses = string.Join(", ", ongoingClasses
                                .Where(c => ongoingClassSubjects.Any(cs => cs.ClassId == c.ClassId && cs.SubjectId == existing.SubjectId))
                                .Select(c => c.ClassCode));

                            throw new BusinessRuleViolationException(
                                $"Không thể thay đổi tiêu chí môn học #{existing.SubjectId} (Điểm đạt/Số giờ/Tính bắt buộc) khi đang có lớp học chưa kết thúc ({affectedClasses}) đang giảng dạy môn này.");
                        }

                        // Update
                        existing.SequenceNo = reqSub.SequenceNo;
                        existing.RequiredHours = reqSub.RequiredHours;
                        existing.RequiredSessions = reqSub.RequiredSessions;
                        existing.IsMandatory = reqSub.IsMandatory;
                        existing.PassingScore = reqSub.PassingScore;
                        existing.SubjectVersion = reqSub.SubjectVersion;
                        _unitOfWork.CourseSubjectRepository.Update(existing);

                        finalSubjects.Add(new CourseSubjectResponse(
                            id, existing.SubjectId, existing.SequenceNo, existing.RequiredHours, existing.RequiredSessions, existing.IsMandatory, existing.PassingScore, existing.SubjectVersion
                        ));
                    }
                    else
                    {
                        // Check if soft-deleted mapping exists to reactivate
                        var softDeleted = (await _unitOfWork.CourseSubjectRepository.GetAllIncludingDeletedAsync(ct))
                            .FirstOrDefault(cs => cs.CourseId == id && cs.SubjectId == reqSub.SubjectId && cs.IsDeleted);

                        if (softDeleted != null)
                        {
                            softDeleted.IsDeleted = false;
                            softDeleted.DeletedAt = null;
                            softDeleted.SequenceNo = reqSub.SequenceNo;
                            softDeleted.RequiredHours = reqSub.RequiredHours;
                            softDeleted.RequiredSessions = reqSub.RequiredSessions;
                            softDeleted.IsMandatory = reqSub.IsMandatory;
                            softDeleted.PassingScore = reqSub.PassingScore;
                            softDeleted.SubjectVersion = reqSub.SubjectVersion;
                            _unitOfWork.CourseSubjectRepository.Update(softDeleted);

                            await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                            {
                                AccountId = updatedByAccountId,
                                ActionType = AuditActionType.UPDATE.ToString(),
                                EntityName = nameof(CourseSubject),
                                RecordId = course.CourseId,
                                NewValue = $"SubjectId: {reqSub.SubjectId}, Seq: {reqSub.SequenceNo} (Reactivated)",
                                Description = $"Reactivated Subject #{reqSub.SubjectId} in Course #{course.CourseId} during full sync"
                            }, ct);

                            finalSubjects.Add(new CourseSubjectResponse(
                                id, reqSub.SubjectId, reqSub.SequenceNo, reqSub.RequiredHours, reqSub.RequiredSessions, reqSub.IsMandatory, reqSub.PassingScore, reqSub.SubjectVersion
                            ));
                        }
                        else
                        {
                            // Add new
                            var subject = await _unitOfWork.SubjectRepository.GetByIdAsync(reqSub.SubjectId, ct)
                                ?? throw new KeyNotFoundException($"Subject {reqSub.SubjectId} not found.");

                            var newCourseSub = new CourseSubject
                            {
                                CourseId = id,
                                SubjectId = reqSub.SubjectId,
                                SequenceNo = reqSub.SequenceNo,
                                RequiredHours = reqSub.RequiredHours,
                                RequiredSessions = reqSub.RequiredSessions,
                                IsMandatory = reqSub.IsMandatory,
                                PassingScore = reqSub.PassingScore,
                                SubjectVersion = reqSub.SubjectVersion,
                                CreatedAt = DateTime.UtcNow,
                                CreatedByAccountId = updatedByAccountId
                            };
                            await _unitOfWork.CourseSubjectRepository.AddAsync(newCourseSub, ct);

                            await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                            {
                                AccountId = updatedByAccountId,
                                ActionType = AuditActionType.INSERT.ToString(),
                                EntityName = nameof(CourseSubject),
                                RecordId = course.CourseId,
                                NewValue = $"SubjectId: {reqSub.SubjectId}, Seq: {reqSub.SequenceNo}",
                                Description = $"Assigned Subject #{reqSub.SubjectId} to Course #{course.CourseId} during full sync"
                            }, ct);

                            finalSubjects.Add(new CourseSubjectResponse(
                                id, reqSub.SubjectId, reqSub.SequenceNo, reqSub.RequiredHours, reqSub.RequiredSessions, reqSub.IsMandatory, reqSub.PassingScore, reqSub.SubjectVersion
                            ));
                        }
                    }
                }

                // Tự động đồng bộ môn học mới và sinh session cho các lớp thuộc khóa học CHƯA CÓ HỌC VIÊN ENROLL
                var activeClasses = (await _unitOfWork.ClassRepository.GetAllAsync(ct))
                    .Where(c => c.CourseId == id && !c.IsDeleted && c.Status != ClassStatus.Cancelled && c.Status != ClassStatus.Completed)
                    .ToList();

                if (activeClasses.Any())
                {
                    var allEnrollments = await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(ct);
                    var allClassSubjects = await _unitOfWork.ClassSubjectRepository.GetAllAsync(ct);
                    var allSessions = await _unitOfWork.SessionRepository.GetAllAsync(ct);
                    var allSubjects = await _unitOfWork.SubjectRepository.GetAllAsync(ct);
                    var subjectMap = allSubjects.ToDictionary(s => s.SubjectId, s => s.SubjectType);
                    var courseSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(ct))
                        .Where(cs => cs.CourseId == id && !cs.IsDeleted)
                        .ToList();

                    foreach (var cls in activeClasses)
                    {
                        bool hasEnrolledStudents = allEnrollments.Any(e => e.ClassId == cls.ClassId && !e.IsDeleted && e.Status != EnrollmentStatus.Withdrawn && e.Status != EnrollmentStatus.Deleted);
                        if (!hasEnrolledStudents)
                        {
                            // 1. Thêm ClassSubject cho những môn mới chưa có trong lớp
                            var existingSubIds = allClassSubjects
                                .Where(cs => cs.ClassId == cls.ClassId && !cs.IsDeleted)
                                .Select(cs => cs.SubjectId)
                                .ToHashSet();

                            var missingCourseSubs = courseSubjects
                                .Where(cs => !existingSubIds.Contains(cs.SubjectId))
                                .ToList();

                            foreach (var mcs in missingCourseSubs)
                            {
                                var newClassSub = new ClassSubject
                                {
                                    ClassId = cls.ClassId,
                                    SubjectId = mcs.SubjectId,
                                    InstructorAccountId = null
                                };
                                await _unitOfWork.ClassSubjectRepository.AddAsync(newClassSub, ct);
                            }

                            // 2. Sinh Sessions cho những môn chưa có session
                            var existingSessionSubIds = allSessions
                                .Where(s => s.ClassId == cls.ClassId && !s.IsDeleted)
                                .Select(s => s.SubjectId)
                                .ToHashSet();

                            var subsNeedingSessions = courseSubjects
                                .Where(cs => !existingSessionSubIds.Contains(cs.SubjectId))
                                .OrderBy(cs => cs.SequenceNo)
                                .ToList();

                            if (subsNeedingSessions.Any())
                            {
                                var existingClassSessions = allSessions
                                    .Where(s => s.ClassId == cls.ClassId && !s.IsDeleted)
                                    .ToList();
                                var currentDate = cls.StartDate.Date;
                                if (existingClassSessions.Any(s => s.SessionDate.HasValue))
                                {
                                    var maxDate = existingClassSessions.Where(s => s.SessionDate.HasValue).Max(s => s.SessionDate!.Value.Date);
                                    currentDate = maxDate.AddDays(1);
                                    if (currentDate.DayOfWeek == DayOfWeek.Sunday) currentDate = currentDate.AddDays(1);
                                }

                                int maxSessionIndex = existingClassSessions.Count;

                                var assessments = (await _unitOfWork.AssessmentRepository.GetAllAsync(ct))
                                    .Where(a => a.CourseId == cls.CourseId && !a.IsDeleted)
                                    .ToList();
                                var checklists = (await _unitOfWork.PracticalChecklistRepository.GetAllAsync(ct))
                                    .Where(pc => (pc.CourseId == cls.CourseId || pc.CourseId == 0) && !pc.IsDeleted)
                                    .ToList();

                                foreach (var cs in subsNeedingSessions)
                                {
                                    subjectMap.TryGetValue(cs.SubjectId, out var subjectType);
                                    var currentSubject = allSubjects.FirstOrDefault(s => s.SubjectId == cs.SubjectId);
                                    TrainingType trainingType = TrainingTypeClassifier.Classify(currentSubject?.SubjectCode, currentSubject?.SubjectName, subjectType);
                                    bool isWorkshopOrNonFstdPractical = FacilityCompatibilityHelper.IsWorkshopOrPracticalSubject(
                                        currentSubject?.SubjectCode, currentSubject?.SubjectName, subjectType);
                                    var compatTypes = FacilityCompatibilityHelper.GetCompatibleFacilityTypes(trainingType, isWorkshopOrNonFstdPractical);

                                    int sessionCount = cs.RequiredSessions > 0 ? cs.RequiredSessions : 1;

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

                                    var subjectAssessment = trainingType == TrainingType.Theory
                                        ? (subjectAssessments.FirstOrDefault(a => string.Equals(a.AssessmentType, "Theory", StringComparison.OrdinalIgnoreCase)) ?? subjectAssessments.FirstOrDefault())
                                        : subjectAssessments.FirstOrDefault(a => string.Equals(a.AssessmentType, "Practical", StringComparison.OrdinalIgnoreCase));
                                    var subjectChecklist = subjectChecklists.FirstOrDefault();

                                    for (int i = 1; i <= sessionCount; i++)
                                    {
                                        DateTime sessionDate = currentDate <= cls.EndDate.Date ? currentDate : cls.EndDate.Date;
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

                                        if (isFinalSession && subjectChecklist != null && trainingType != TrainingType.Theory)
                                        {
                                            checklistId = subjectChecklist.PracticalChecklistId;
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

                                        DateTime sessionStart = sessionDate.Date.AddHours(8);
                                        DateTime sessionEnd = sessionStart.AddHours(2);

                                        var allFacilities = (await _unitOfWork.TrainingFacilityRepository.GetAllAsync(ct))
                                            .Where(f => !f.IsDeleted && f.IsActive)
                                            .ToList();

                                        TrainingFacility? assignedFacility = null;
                                        if (cls.DefaultFacilityId.HasValue)
                                        {
                                            var defFac = allFacilities.FirstOrDefault(f => f.FacilityId == cls.DefaultFacilityId.Value);
                                            if (defFac != null && compatTypes.Contains(defFac.FacilityType))
                                            {
                                                bool conflict = allSessions.Any(s =>
                                                    s.FacilityId == defFac.FacilityId &&
                                                    FacilityCompatibilityHelper.HasTimeOverlap(
                                                        sessionStart, sessionEnd,
                                                        s.StartAt ?? s.SessionDate ?? DateTime.MinValue,
                                                        s.EndAt ?? (s.SessionDate.HasValue ? s.SessionDate.Value.AddHours(2) : DateTime.MinValue)));

                                                if (!conflict) assignedFacility = defFac;
                                            }
                                        }

                                        if (assignedFacility == null)
                                        {
                                            foreach (var fac in allFacilities.Where(f => compatTypes.Contains(f.FacilityType)))
                                            {
                                                bool conflict = allSessions.Any(s =>
                                                    s.FacilityId == fac.FacilityId &&
                                                    FacilityCompatibilityHelper.HasTimeOverlap(
                                                        sessionStart, sessionEnd,
                                                        s.StartAt ?? s.SessionDate ?? DateTime.MinValue,
                                                        s.EndAt ?? (s.SessionDate.HasValue ? s.SessionDate.Value.AddHours(2) : DateTime.MinValue)));

                                                if (!conflict)
                                                {
                                                    assignedFacility = fac;
                                                    break;
                                                }
                                            }
                                        }

                                        int? facilityId = assignedFacility?.FacilityId;
                                        string? sessionLocation = assignedFacility != null 
                                            ? assignedFacility.FacilityName 
                                            : (!string.IsNullOrWhiteSpace(cls.Location) ? cls.Location : "Chưa xếp cơ sở (TBA)");

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
                                            IsConfirmed = false,
                                            IsAssessmentRequired = isAssessmentRequired,
                                            AssessmentId = assessmentId,
                                            IsChecklistRequired = isChecklistRequired,
                                            PracticalChecklistId = checklistId,
                                            TrainingType = trainingType,
                                            LessonCode = null
                                        };

                                        await _unitOfWork.SessionRepository.AddAsync(session, ct);
                                        allSessions.Add(session);

                                        currentDate = currentDate.AddDays(1);
                                        if (currentDate.DayOfWeek == DayOfWeek.Sunday)
                                        {
                                            currentDate = currentDate.AddDays(1);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // SYNC COURSE DEPARTMENTS
                var deptIdsList = new List<int>();
                var deptNamesList = new List<string>();
                if (request.DepartmentIds != null && _unitOfWork.CourseDepartmentRepository != null && _unitOfWork.DepartmentRepository != null)
                {
                    var validatedDepts = await ValidateAndGetTrainingAudienceDepartmentsAsync(request.DepartmentIds, ct);
                    var targetDeptDict = validatedDepts.ToDictionary(d => d.DepartmentId, d => d.DepartmentName);
                    var targetDeptIds = targetDeptDict.Keys.ToHashSet();

                    var existingCourseDepts = (await _unitOfWork.CourseDepartmentRepository.GetAllAsync(ct))
                        .Where(cd => cd.CourseId == id).ToList();

                    foreach (var cd in existingCourseDepts)
                    {
                        if (!targetDeptIds.Contains(cd.DepartmentId))
                        {
                            cd.IsDeleted = true;
                            cd.DeletedAt = DateTime.UtcNow;
                            cd.UpdatedAt = DateTime.UtcNow;
                            cd.UpdatedByAccountId = updatedByAccountId;
                            _unitOfWork.CourseDepartmentRepository.Update(cd);
                        }
                    }

                    foreach (var deptId in targetDeptIds)
                    {
                        var existing = existingCourseDepts.FirstOrDefault(cd => cd.DepartmentId == deptId);
                        if (existing != null)
                        {
                            if (existing.IsDeleted)
                            {
                                existing.IsDeleted = false;
                                existing.DeletedAt = null;
                                existing.UpdatedAt = DateTime.UtcNow;
                                existing.UpdatedByAccountId = updatedByAccountId;
                                _unitOfWork.CourseDepartmentRepository.Update(existing);
                            }
                        }
                        else
                        {
                            var newCd = new CourseDepartment
                            {
                                CourseId = id,
                                DepartmentId = deptId,
                                CreatedAt = DateTime.UtcNow,
                                CreatedByAccountId = updatedByAccountId
                            };
                            await _unitOfWork.CourseDepartmentRepository.AddAsync(newCd, ct);
                        }
                        deptIdsList.Add(deptId);
                        deptNamesList.Add(targetDeptDict[deptId]);
                    }
                }
                else if (_unitOfWork.CourseDepartmentRepository != null && _unitOfWork.DepartmentRepository != null)
                {
                    var existingCourseDepts = (await _unitOfWork.CourseDepartmentRepository.GetAllAsync(ct))
                        .Where(cd => cd.CourseId == id && !cd.IsDeleted).ToList();
                    var allDepartments = (await _unitOfWork.DepartmentRepository.GetAllAsync(ct))
                        .ToDictionary(d => d.DepartmentId, d => d.DepartmentName);
                    foreach (var cd in existingCourseDepts)
                    {
                        deptIdsList.Add(cd.DepartmentId);
                        if (allDepartments.TryGetValue(cd.DepartmentId, out var dn))
                        {
                            deptNamesList.Add(dn);
                        }
                    }
                }

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return new CourseResponse(
                    course.CourseId,
                    course.CourseCode,
                    course.CourseName,
                    course.Description,
                    course.DurationHours,
                    course.Status,
                    course.ValidityMonths,
                    course.CourseType,
                    finalSubjects.OrderBy(s => s.SequenceNo).ToList(),
                    course.VersionNo,
                    DepartmentIds: deptIdsList.Count > 0 ? deptIdsList : null,
                    DepartmentNames: deptNamesList.Count > 0 ? deptNamesList : null);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    public async Task<CourseResponse> CloneCourseVersionAsync(int courseId, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInStrategyAsync(async (ct) =>
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var original = await _unitOfWork.CourseRepository.GetByIdAsync(courseId, ct)
                    ?? throw new KeyNotFoundException($"Course {courseId} not found.");

                if (original.IsDeleted) throw new KeyNotFoundException($"Course {courseId} not found.");

                var allVersions = (await _unitOfWork.CourseRepository.GetAllIncludingDeletedAsync(ct))
                    .Where(c => c.CourseCode == original.CourseCode)
                    .ToList();

                var maxVersion = allVersions.Select(c => c.VersionNo).DefaultIfEmpty(0).Max();
                var nextVersionNo = Math.Max(maxVersion + 1, original.VersionNo + 1);

                var newCourse = new Course
                {
                    CourseCode = original.CourseCode,
                    CourseName = original.CourseName,
                    Description = original.Description,
                    DurationHours = original.DurationHours,
                    Status = CourseStatus.Draft,
                    ValidityMonths = original.ValidityMonths,
                    CourseType = original.CourseType,
                    VersionNo = nextVersionNo,
                    PreviousVersionId = original.CourseId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByAccountId = createdByAccountId
                };

                await _unitOfWork.CourseRepository.AddAsync(newCourse, ct);
                await _unitOfWork.SaveAsync(ct);

                // 1. Clone CourseSubject
                var originalSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(ct))
                    .Where(cs => cs.CourseId == courseId && !cs.IsDeleted)
                    .OrderBy(cs => cs.SequenceNo)
                    .ToList();

                var clonedSubjects = new List<CourseSubjectResponse>();
                foreach (var s in originalSubjects)
                {
                    var newCourseSubject = new CourseSubject
                    {
                        CourseId = newCourse.CourseId,
                        SubjectId = s.SubjectId,
                        SequenceNo = s.SequenceNo,
                        RequiredHours = s.RequiredHours,
                        RequiredSessions = s.RequiredSessions,
                        IsMandatory = s.IsMandatory,
                        PassingScore = s.PassingScore,
                        SubjectVersion = s.SubjectVersion,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByAccountId = createdByAccountId
                    };
                    await _unitOfWork.CourseSubjectRepository.AddAsync(newCourseSubject, ct);
                    clonedSubjects.Add(new CourseSubjectResponse(
                        newCourse.CourseId, s.SubjectId, s.SequenceNo, s.RequiredHours, s.RequiredSessions, s.IsMandatory, s.PassingScore, s.SubjectVersion));
                }

                // 2. Clone Assessment
                var originalAssessments = (await _unitOfWork.AssessmentRepository.GetAllAsync(ct))
                    .Where(a => a.CourseId == courseId && !a.IsDeleted)
                    .ToList();

                foreach (var a in originalAssessments)
                {
                    var newAssessment = new Assessment
                    {
                        CourseId = newCourse.CourseId,
                        SubjectId = a.SubjectId,
                        ComponentName = a.ComponentName,
                        AssessmentType = a.AssessmentType,
                        Weight = a.Weight,
                        PassingScore = a.PassingScore,
                        IsRequired = a.IsRequired,
                        DisplayOrder = a.DisplayOrder,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByAccountId = createdByAccountId
                    };
                    await _unitOfWork.AssessmentRepository.AddAsync(newAssessment, ct);
                }

                // 3. Clone PracticalChecklist
                var originalChecklists = (await _unitOfWork.PracticalChecklistRepository.GetAllAsync(ct))
                    .Where(p => p.CourseId == courseId && !p.IsDeleted)
                    .ToList();

                foreach (var p in originalChecklists)
                {
                    var newChecklist = new PracticalChecklist
                    {
                        CourseId = newCourse.CourseId,
                        SubjectId = p.SubjectId,
                        ItemName = p.ItemName,
                        Description = p.Description,
                        IsRequired = p.IsRequired,
                        DisplayOrder = p.DisplayOrder,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByAccountId = createdByAccountId
                    };
                    await _unitOfWork.PracticalChecklistRepository.AddAsync(newChecklist, ct);
                }

                // 4. Clone CompletionRequirement
                var originalRequirements = (await _unitOfWork.CompletionRequirementRepository.GetAllAsync(ct))
                    .Where(cr => cr.CourseId == courseId && !cr.IsDeleted && cr.EffectiveTo == null)
                    .ToList();

                foreach (var cr in originalRequirements)
                {
                    var newReq = new CompletionRequirement
                    {
                        CourseId = newCourse.CourseId,
                        RequirementName = cr.RequirementName,
                        Description = cr.Description,
                        IsMandatory = cr.IsMandatory,
                        DisplayOrder = cr.DisplayOrder,
                        RequirementType = cr.RequirementType,
                        ThresholdValue = cr.ThresholdValue,
                        VersionNo = nextVersionNo,
                        EffectiveFrom = DateTime.UtcNow,
                        EffectiveTo = null,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByAccountId = createdByAccountId
                    };
                    await _unitOfWork.CompletionRequirementRepository.AddAsync(newReq, ct);
                }

                // 5. Clone CourseDepartment
                var clonedDeptIds = new List<int>();
                var clonedDeptNames = new List<string>();
                if (_unitOfWork.CourseDepartmentRepository != null && _unitOfWork.DepartmentRepository != null)
                {
                    var originalCourseDepts = (await _unitOfWork.CourseDepartmentRepository.GetAllAsync(ct))
                        .Where(cd => cd.CourseId == courseId && !cd.IsDeleted)
                        .ToList();

                    var allDepts = (await _unitOfWork.DepartmentRepository.GetAllAsync(ct))
                        .ToDictionary(d => d.DepartmentId, d => d.DepartmentName);

                    foreach (var cd in originalCourseDepts)
                    {
                        var newCd = new CourseDepartment
                        {
                            CourseId = newCourse.CourseId,
                            DepartmentId = cd.DepartmentId,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByAccountId = createdByAccountId
                        };
                        await _unitOfWork.CourseDepartmentRepository.AddAsync(newCd, ct);
                        clonedDeptIds.Add(cd.DepartmentId);
                        if (allDepts.TryGetValue(cd.DepartmentId, out var dn))
                        {
                            clonedDeptNames.Add(dn);
                        }
                    }
                }

                await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                {
                    AccountId = createdByAccountId,
                    ActionType = AuditActionType.INSERT.ToString(),
                    EntityName = nameof(Course),
                    RecordId = newCourse.CourseId,
                    NewValue = $"{newCourse.CourseCode} (v{newCourse.VersionNo})",
                    Description = $"Cloned Course #{original.CourseId} ({original.CourseCode} v{original.VersionNo}) into new version #{newCourse.CourseId} (v{newCourse.VersionNo})"
                }, ct);

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return new CourseResponse(
                    newCourse.CourseId,
                    newCourse.CourseCode,
                    newCourse.CourseName,
                    newCourse.Description,
                    newCourse.DurationHours,
                    newCourse.Status,
                    newCourse.ValidityMonths,
                    newCourse.CourseType,
                    clonedSubjects,
                    newCourse.VersionNo,
                    newCourse.PreviousVersionId,
                    DepartmentIds: clonedDeptIds.Count > 0 ? clonedDeptIds : null,
                    DepartmentNames: clonedDeptNames.Count > 0 ? clonedDeptNames : null
                );
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    public async Task DeleteCourseAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        var course = await _unitOfWork.CourseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Course not found.");

        if (course.IsDeleted) return;

        await EnsureCourseNotLockedAsync(id, cancellationToken);

        var activeClasses = (await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken))
            .Where(c => c.CourseId == id && !c.IsDeleted && c.Status != ClassStatus.Cancelled).ToList();
        if (activeClasses.Any())
        {
            throw new BusinessRuleViolationException($"Không thể xóa khóa học '{course.CourseName}' vì đang có {activeClasses.Count} lớp học liên kết chưa hủy. Vui lòng hủy hoặc kết thúc các lớp học trước khi xóa khóa.");
        }

        // Soft Delete
        course.IsDeleted = true;
        course.DeletedAt = DateTime.UtcNow;
        course.UpdatedAt = DateTime.UtcNow;
        course.UpdatedByAccountId = deletedByAccountId;

        _unitOfWork.CourseRepository.Update(course);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = deletedByAccountId,
            ActionType = AuditActionType.DELETE.ToString(),
            EntityName = nameof(Course),
            RecordId = course.CourseId,
            OldValue = course.Status.ToString(),
            NewValue = "Deleted",
            Description = $"Course #{course.CourseId} ({course.CourseCode}) deleted"
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);
    }

    public async Task<CourseSubjectResponse> AddSubjectToCourseAsync(int courseId, AddCourseSubjectRequest request, int addedByAccountId, CancellationToken cancellationToken = default)
    {
        var course = await _unitOfWork.CourseRepository.GetByIdAsync(courseId, cancellationToken)
            ?? throw new KeyNotFoundException("Course not found.");

        if (course.IsDeleted) throw new KeyNotFoundException("Course not found.");

        await EnsureCourseNotLockedAsync(courseId, cancellationToken);

        var subject = await _unitOfWork.SubjectRepository.GetByIdAsync(request.SubjectId, cancellationToken)
            ?? throw new KeyNotFoundException("Subject not found.");

        var existingMapping = (await _unitOfWork.CourseSubjectRepository.GetAllIncludingDeletedAsync(cancellationToken))
            .FirstOrDefault(cs => cs.CourseId == courseId && cs.SubjectId == request.SubjectId);

        if (existingMapping != null && !existingMapping.IsDeleted)
        {
            throw new InvalidOperationException($"Subject {request.SubjectId} is already assigned to Course {courseId}.");
        }

        CourseSubject courseSubject;
        if (existingMapping != null && existingMapping.IsDeleted)
        {
            existingMapping.IsDeleted = false;
            existingMapping.DeletedAt = null;
            existingMapping.SequenceNo = request.SequenceNo;
            existingMapping.RequiredHours = request.RequiredHours;
            existingMapping.RequiredSessions = request.RequiredSessions;
            existingMapping.IsMandatory = request.IsMandatory;
            existingMapping.PassingScore = request.PassingScore;
            existingMapping.SubjectVersion = request.SubjectVersion;

            _unitOfWork.CourseSubjectRepository.Update(existingMapping);
            courseSubject = existingMapping;

            await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
            {
                AccountId = addedByAccountId,
                ActionType = AuditActionType.UPDATE.ToString(),
                EntityName = nameof(CourseSubject),
                RecordId = courseId,
                NewValue = $"SubjectId: {request.SubjectId}, Seq: {request.SequenceNo} (Reactivated)",
                Description = $"Reactivated Subject #{request.SubjectId} in Course #{courseId}"
            }, cancellationToken);
        }
        else
        {
            courseSubject = new CourseSubject
            {
                CourseId = courseId,
                SubjectId = request.SubjectId,
                SequenceNo = request.SequenceNo,
                RequiredHours = request.RequiredHours,
                RequiredSessions = request.RequiredSessions,
                IsMandatory = request.IsMandatory,
                PassingScore = request.PassingScore,
                SubjectVersion = request.SubjectVersion,
                CreatedAt = DateTime.UtcNow,
                CreatedByAccountId = addedByAccountId
            };

            await _unitOfWork.CourseSubjectRepository.AddAsync(courseSubject, cancellationToken);

            await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
            {
                AccountId = addedByAccountId,
                ActionType = AuditActionType.INSERT.ToString(),
                EntityName = nameof(CourseSubject),
                RecordId = courseId,
                NewValue = $"SubjectId: {request.SubjectId}, Seq: {request.SequenceNo}",
                Description = $"Assigned Subject #{request.SubjectId} to Course #{courseId}"
            }, cancellationToken);
        }

        await _unitOfWork.SaveAsync(cancellationToken);

        return new CourseSubjectResponse(
            courseSubject.CourseId,
            courseSubject.SubjectId,
            courseSubject.SequenceNo,
            courseSubject.RequiredHours,
            courseSubject.RequiredSessions,
            courseSubject.IsMandatory,
            courseSubject.PassingScore,
            courseSubject.SubjectVersion
        );
    }

    public async Task<IEnumerable<CourseSubjectResponse>> GetSubjectsByCourseAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var courseSubjects = await _unitOfWork.CourseSubjectRepository.GetAllAsync(cancellationToken);
        return courseSubjects
            .Where(cs => cs.CourseId == courseId)
            .OrderBy(cs => cs.SequenceNo)
            .Select(cs => new CourseSubjectResponse(
                cs.CourseId,
                cs.SubjectId,
                cs.SequenceNo,
                cs.RequiredHours,
                cs.RequiredSessions,
                cs.IsMandatory,
                cs.PassingScore,
                cs.SubjectVersion
            ));
    }

    public async Task<CourseSubjectResponse> UpdateCourseSubjectAsync(int courseId, int subjectId, UpdateCourseSubjectRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        await EnsureCourseNotLockedAsync(courseId, cancellationToken);

        var existingMapping = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(cs => cs.CourseId == courseId && cs.SubjectId == subjectId)
            ?? throw new KeyNotFoundException("CourseSubject mapping not found.");

        // Validate: Check if there are ongoing/scheduled classes in this course that include this subject
        var ongoingClasses = (await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken))
            .Where(c => c.CourseId == courseId && !c.IsDeleted && (c.Status == ClassStatus.InProgress || c.Status == ClassStatus.Scheduled))
            .ToList();

        if (ongoingClasses.Any())
        {
            var ongoingClassIds = ongoingClasses.Select(c => c.ClassId).ToHashSet();
            var isSubjectInOngoing = (await _unitOfWork.ClassSubjectRepository.GetAllAsync(cancellationToken))
                .Any(cs => ongoingClassIds.Contains(cs.ClassId) && cs.SubjectId == subjectId && !cs.IsDeleted);

            if (isSubjectInOngoing)
            {
                bool isChangingCriteria = existingMapping.PassingScore != request.PassingScore ||
                                          existingMapping.RequiredHours != request.RequiredHours ||
                                          existingMapping.RequiredSessions != request.RequiredSessions ||
                                          existingMapping.IsMandatory != request.IsMandatory;

                if (isChangingCriteria)
                {
                    var classCodes = string.Join(", ", ongoingClasses.Select(c => c.ClassCode));
                    throw new BusinessRuleViolationException(
                        $"Không thể thay đổi tiêu chí đánh giá môn học (Điểm đạt/Số giờ/Tính bắt buộc) khi đang có lớp học chưa kết thúc ({classCodes}) đang giảng dạy môn học này.");
                }
            }
        }

        existingMapping.SequenceNo = request.SequenceNo;
        existingMapping.RequiredHours = request.RequiredHours;
        existingMapping.RequiredSessions = request.RequiredSessions;
        existingMapping.IsMandatory = request.IsMandatory;
        existingMapping.PassingScore = request.PassingScore;
        existingMapping.SubjectVersion = request.SubjectVersion;

        _unitOfWork.CourseSubjectRepository.Update(existingMapping);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = updatedByAccountId,
            ActionType = AuditActionType.UPDATE.ToString(),
            EntityName = nameof(CourseSubject),
            RecordId = courseId,
            NewValue = $"Seq: {request.SequenceNo}, Pass: {request.PassingScore}",
            Description = $"Updated Subject #{subjectId} in Course #{courseId}"
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        return new CourseSubjectResponse(
            existingMapping.CourseId,
            existingMapping.SubjectId,
            existingMapping.SequenceNo,
            existingMapping.RequiredHours,
            existingMapping.RequiredSessions,
            existingMapping.IsMandatory,
            existingMapping.PassingScore,
            existingMapping.SubjectVersion
        );
    }

    public async Task RemoveSubjectFromCourseAsync(int courseId, int subjectId, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
        await EnsureCourseNotLockedAsync(courseId, cancellationToken);

        var existingMapping = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(cs => cs.CourseId == courseId && cs.SubjectId == subjectId)
            ?? throw new KeyNotFoundException("CourseSubject mapping not found.");

        // Check if there are classes under this course using this subject
        var nonCancelledClasses = (await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken))
            .Where(c => c.CourseId == courseId && !c.IsDeleted && c.Status != ClassStatus.Cancelled)
            .ToList();

        if (nonCancelledClasses.Any())
        {
            var classIdsInCourse = nonCancelledClasses.Select(c => c.ClassId).ToHashSet();
            var classSubjects = (await _unitOfWork.ClassSubjectRepository.GetAllAsync(cancellationToken))
                .Where(cs => classIdsInCourse.Contains(cs.ClassId) && cs.SubjectId == subjectId && !cs.IsDeleted)
                .ToList();

            if (classSubjects.Any())
            {
                var assignedClassIds = classSubjects.Select(cs => cs.ClassId).ToHashSet();
                var ongoingClasses = nonCancelledClasses
                    .Where(c => assignedClassIds.Contains(c.ClassId) && (c.Status == ClassStatus.InProgress || c.Status == ClassStatus.Scheduled))
                    .Select(c => c.ClassCode)
                    .Distinct()
                    .ToList();

                if (ongoingClasses.Any())
                {
                    throw new BusinessRuleViolationException(
                        $"Không thể gỡ môn học khỏi khóa học vì đang có lớp học chưa kết thúc ({string.Join(", ", ongoingClasses)}) đang giảng dạy môn học này.");
                }

                throw new BusinessRuleViolationException(
                    "Không thể gỡ môn học khỏi khóa học vì đã có lớp học thuộc khóa đang giảng dạy hoặc đã hoàn thành môn học này.");
            }
        }
            
        // CourseSubject is a mapping table with soft-delete
        existingMapping.IsDeleted = true;
        existingMapping.DeletedAt = DateTime.UtcNow;
        _unitOfWork.CourseSubjectRepository.Update(existingMapping);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = deletedByAccountId,
            ActionType = AuditActionType.DELETE.ToString(),
            EntityName = nameof(CourseSubject),
            RecordId = courseId,
            OldValue = $"SubjectId: {subjectId}",
            NewValue = "Deleted",
            Description = $"Removed Subject #{subjectId} from Course #{courseId}"
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);
    }

    private async Task<List<Department>> ValidateAndGetTrainingAudienceDepartmentsAsync(List<int> departmentIds, CancellationToken ct)
    {
        if (departmentIds == null || !departmentIds.Any() || _unitOfWork.DepartmentRepository == null)
        {
            return new List<Department>();
        }

        var distinctIds = departmentIds.Distinct().ToList();
        var allDepts = (await _unitOfWork.DepartmentRepository.GetAllAsync(ct))?.ToList() ?? new List<Department>();
        var deptDict = allDepts.ToDictionary(d => d.DepartmentId, d => d);

        var validDepartments = new List<Department>();
        foreach (var id in distinctIds)
        {
            if (!deptDict.TryGetValue(id, out var dept) || dept.IsDeleted)
            {
                throw new BusinessRuleViolationException($"Phòng ban (ID: #{id}) không tồn tại trong hệ thống.");
            }

            if (!dept.IsTrainingAudience)
            {
                throw new BusinessRuleViolationException($"Phòng ban '{dept.DepartmentName}' (Mã: {dept.DepartmentCode}) là phòng ban nội bộ/vận hành, không thể cấu hình làm đối tượng đào tạo cho khóa học.");
            }

            validDepartments.Add(dept);
        }

        return validDepartments;
    }
}
