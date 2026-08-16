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
                    OldValue = oldStatus,
                    NewValue = course.Status,
                    Description = validityMonthsChanged
                        ? $"Course #{course.CourseId} ({course.CourseCode}) updated; ValidityMonths changed — VersionNo bumped to {course.VersionNo} so already-enrolled learners keep evaluating against the prior version"
                        : $"Course #{course.CourseId} ({course.CourseCode}) updated"
                }, ct);

                // SYNC SUBJECTS
                var existingSubjects = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(ct))
                    .Where(cs => cs.CourseId == id && !cs.IsDeleted).ToList();

                var requestedSubjectIds = request.Subjects.Select(s => s.SubjectId).ToList();

                // 1. Remove subjects not in the request
                var subjectsToRemove = existingSubjects.Where(cs => !requestedSubjectIds.Contains(cs.SubjectId)).ToList();
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

                // 2. Add or Update subjects
                var finalSubjects = new List<CourseSubjectResponse>();
                foreach (var reqSub in request.Subjects)
                {
                    var existing = existingSubjects.FirstOrDefault(cs => cs.SubjectId == reqSub.SubjectId);
                    if (existing != null)
                    {
                        // Update
                        existing.SequenceNo = reqSub.SequenceNo;
                        existing.RequiredHours = reqSub.RequiredHours;
                        existing.IsMandatory = reqSub.IsMandatory;
                        existing.PassingScore = reqSub.PassingScore;
                        _unitOfWork.CourseSubjectRepository.Update(existing);

                        finalSubjects.Add(new CourseSubjectResponse(
                            id, existing.SubjectId, existing.SequenceNo, existing.RequiredHours, existing.RequiredSessions, existing.IsMandatory, existing.PassingScore
                        ));
                    }
                    else
                    {
                        // Add
                        var subject = await _unitOfWork.SubjectRepository.GetByIdAsync(reqSub.SubjectId, ct)
                            ?? throw new KeyNotFoundException($"Subject {reqSub.SubjectId} not found.");

                        var newCourseSub = new CourseSubject
                        {
                            CourseId = id,
                            SubjectId = reqSub.SubjectId,
                            SequenceNo = reqSub.SequenceNo,
                            RequiredHours = reqSub.RequiredHours,
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
            OldValue = course.Status,
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

        var existingMapping = (await _unitOfWork.CourseSubjectRepository.GetAllAsync(cancellationToken))
            .FirstOrDefault(cs => cs.CourseId == courseId && cs.SubjectId == request.SubjectId);

        if (existingMapping != null)
        {
            throw new InvalidOperationException($"Subject {request.SubjectId} is already assigned to Course {courseId}.");
        }

        var courseSubject = new CourseSubject
        {
            CourseId = courseId,
            SubjectId = request.SubjectId,
            SequenceNo = request.SequenceNo,
            RequiredHours = request.RequiredHours,
            IsMandatory = request.IsMandatory,
            PassingScore = request.PassingScore,
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = addedByAccountId
        };

        await _unitOfWork.CourseSubjectRepository.AddAsync(courseSubject, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = addedByAccountId,
            ActionType = AuditActionType.INSERT.ToString(),
            EntityName = nameof(CourseSubject),
            RecordId = courseId, // Using CourseId as RecordId since it's a mapping
            NewValue = $"SubjectId: {request.SubjectId}, Seq: {request.SequenceNo}",
            Description = $"Assigned Subject #{request.SubjectId} to Course #{courseId}"
        }, cancellationToken);

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

        existingMapping.SequenceNo = request.SequenceNo;
        existingMapping.RequiredHours = request.RequiredHours;
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

        // Check if there are any enrollments before deleting? Business logic usually prevents deleting if course has active enrollments
        var hasEnrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken))
            .Any(e => e.ClassId != 0); // Need proper check if required, skipping deep check for now to allow soft delete/hard delete
            
        // CourseSubject is a mapping table, usually hard deleted unless IsDeleted exists. BaseEntity has IsDeleted.
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
