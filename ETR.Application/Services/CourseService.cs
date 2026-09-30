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

    public async Task<IEnumerable<CourseResponse>> GetAllCoursesAsync(CancellationToken cancellationToken = default)
    {
        var courses = await _unitOfWork.CourseRepository.GetAllAsync(cancellationToken);
        return courses.Where(c => !c.IsDeleted).Select(c => new CourseResponse(
            c.CourseId, c.CourseCode, c.CourseName, c.Description, c.DurationHours, c.Status, c.ValidityMonths, c.CourseType, VersionNo: c.VersionNo));
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
                cs.CourseId, cs.SubjectId, cs.SequenceNo, cs.RequiredHours, cs.RequiredSessions, cs.IsMandatory, cs.PassingScore
            )).ToList();

        return new CourseResponse(c.CourseId, c.CourseCode, c.CourseName, c.Description, c.DurationHours, c.Status, c.ValidityMonths, c.CourseType, subjects, c.VersionNo);
    }

    public async Task<CourseResponse> CreateCourseAsync(CreateCourseRequest request, int createdByAccountId, CancellationToken cancellationToken = default)
    {
        if (request.Subjects == null || !request.Subjects.Any())
        {
            throw new ArgumentException("A course must have at least one subject configured upon creation.");
        }

        var isDuplicate = _unitOfWork.CourseRepository.GetQueryable()
            .Any(c => c.CourseCode == request.CourseCode && !c.IsDeleted);
        if (isDuplicate)
        {
            throw new BusinessRuleViolationException($"A course with code '{request.CourseCode}' already exists.");
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
                        course.CourseId, s.SubjectId, s.SequenceNo, s.RequiredHours, s.RequiredSessions, s.IsMandatory, s.PassingScore
                    ));
                }

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return new CourseResponse(course.CourseId, course.CourseCode, course.CourseName, course.Description, course.DurationHours, course.Status, course.ValidityMonths, course.CourseType, responseSubjects, course.VersionNo);
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

                var isDuplicate = _unitOfWork.CourseRepository.GetQueryable()
                    .Any(c => c.CourseId != id && c.CourseCode == request.CourseCode && !c.IsDeleted);
                if (isDuplicate)
                {
                    throw new BusinessRuleViolationException($"A course with code '{request.CourseCode}' already exists.");
                }

                var oldStatus = course.Status;

                // ValidityMonths is the only Course field an evaluation rule actually reads today
                // (it drives ETRCourseRecord.ExpiryDate at Completion — see EtrService.CompleteEtrAsync).
                // Changing it must NOT retroactively affect learners who already enrolled under the
                // old value, so it bumps VersionNo instead of silently mutating in place; other
                // fields (name/description/status/duration/type) are purely descriptive and stay a
                // plain overwrite.
                var validityMonthsChanged = course.ValidityMonths != request.ValidityMonths;

                course.CourseCode = request.CourseCode;
                course.CourseName = request.CourseName;
                course.Description = request.Description;
                course.DurationHours = request.DurationHours;
                course.Status = request.Status;
                course.ValidityMonths = request.ValidityMonths;
                course.CourseType = request.CourseType;
                course.UpdatedAt = DateTime.UtcNow;
                course.UpdatedByAccountId = updatedByAccountId;

                if (validityMonthsChanged)
                {
                    course.VersionNo += 1;
                    course.EffectiveFrom = DateTime.UtcNow;
                }

                _unitOfWork.CourseRepository.Update(course);

                await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                {
                    AccountId = updatedByAccountId,
                    ActionType = AuditActionType.UPDATE.ToString(),
                    EntityName = nameof(Course),
                    RecordId = course.CourseId,
                    OldValue = oldStatus.ToString(),
                    NewValue = course.Status.ToString(),
                    Description = validityMonthsChanged
                        ? $"Course #{course.CourseId} ({course.CourseCode}) updated; ValidityMonths changed — VersionNo bumped to {course.VersionNo} so already-enrolled learners keep evaluating against the prior version"
                        : $"Course #{course.CourseId} ({course.CourseCode}) updated"
                }, ct);

                // SYNC SUBJECTS
                var existingSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(ct))
                    .Where(cs => cs.CourseId == id && !cs.IsDeleted).ToList();

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
                        _unitOfWork.CourseSubjectRepository.Update(existing);

                        finalSubjects.Add(new CourseSubjectResponse(
                            id, existing.SubjectId, existing.SequenceNo, existing.RequiredHours, existing.RequiredSessions, existing.IsMandatory, existing.PassingScore
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
                                id, reqSub.SubjectId, reqSub.SequenceNo, reqSub.RequiredHours, reqSub.RequiredSessions, reqSub.IsMandatory, reqSub.PassingScore
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
                                id, reqSub.SubjectId, reqSub.SequenceNo, reqSub.RequiredHours, reqSub.RequiredSessions, reqSub.IsMandatory, reqSub.PassingScore
                            ));
                        }
                    }
                }

                await _unitOfWork.SaveAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                return new CourseResponse(course.CourseId, course.CourseCode, course.CourseName, course.Description, course.DurationHours, course.Status, course.ValidityMonths, course.CourseType, finalSubjects.OrderBy(s => s.SequenceNo).ToList(), course.VersionNo);
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
            courseSubject.PassingScore
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
                cs.PassingScore
            ));
    }

    public async Task<CourseSubjectResponse> UpdateCourseSubjectAsync(int courseId, int subjectId, UpdateCourseSubjectRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
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
            existingMapping.PassingScore
        );
    }

    public async Task RemoveSubjectFromCourseAsync(int courseId, int subjectId, int deletedByAccountId, CancellationToken cancellationToken = default)
    {
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
}
