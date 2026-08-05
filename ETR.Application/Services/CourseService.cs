using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;

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
            c.CourseId, c.CourseCode, c.CourseName, c.Description, c.DurationHours, c.Status, c.ValidityMonths, c.CourseType));
    }

    public async Task<CourseResponse> GetCourseByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var c = await _unitOfWork.CourseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Course not found.");

        if (c.IsDeleted) throw new KeyNotFoundException("Course not found.");

        return new CourseResponse(c.CourseId, c.CourseCode, c.CourseName, c.Description, c.DurationHours, c.Status, c.ValidityMonths, c.CourseType);
    }

    public async Task<CourseResponse> CreateCourseAsync(CreateCourseRequest request, int createdByAccountId, CancellationToken cancellationToken = default)
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

        await _unitOfWork.CourseRepository.AddAsync(course, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            AccountId = createdByAccountId,
            ActionType = "INSERT",
            EntityName = nameof(Course),
            RecordId = course.CourseId,
            NewValue = course.CourseCode,
            Description = $"Course #{course.CourseId} ({course.CourseCode}) created"
        }, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CourseResponse(course.CourseId, course.CourseCode, course.CourseName, course.Description, course.DurationHours, course.Status, course.ValidityMonths, course.CourseType);
    }

    public async Task<CourseResponse> UpdateCourseAsync(int id, UpdateCourseRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var course = await _unitOfWork.CourseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Course not found.");

        if (course.IsDeleted) throw new KeyNotFoundException("Course not found.");

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
            ActionType = "UPDATE",
            EntityName = nameof(Course),
            RecordId = course.CourseId,
            OldValue = oldStatus,
            NewValue = course.Status,
            Description = $"Course #{course.CourseId} ({course.CourseCode}) updated"
        }, cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        return new CourseResponse(course.CourseId, course.CourseCode, course.CourseName, course.Description, course.DurationHours, course.Status, course.ValidityMonths, course.CourseType);
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
            ActionType = "DELETE",
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
            ActionType = "INSERT",
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
            courseSubject.IsMandatory,
            courseSubject.PassingScore
        );
    }
}
