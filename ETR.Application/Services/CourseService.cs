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
            c.CourseId, c.CourseCode, c.CourseName, c.Description, c.DurationHours, c.Status));
    }

    public async Task<CourseResponse> GetCourseByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var c = await _unitOfWork.CourseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Course not found.");

        if (c.IsDeleted) throw new KeyNotFoundException("Course not found.");

        return new CourseResponse(c.CourseId, c.CourseCode, c.CourseName, c.Description, c.DurationHours, c.Status);
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
            CreatedAt = DateTime.UtcNow,
            CreatedByAccountId = createdByAccountId
        };

        await _unitOfWork.CourseRepository.AddAsync(course, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CourseResponse(course.CourseId, course.CourseCode, course.CourseName, course.Description, course.DurationHours, course.Status);
    }

    public async Task<CourseResponse> UpdateCourseAsync(int id, UpdateCourseRequest request, int updatedByAccountId, CancellationToken cancellationToken = default)
    {
        var course = await _unitOfWork.CourseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Course not found.");

        if (course.IsDeleted) throw new KeyNotFoundException("Course not found.");

        course.CourseCode = request.CourseCode;
        course.CourseName = request.CourseName;
        course.Description = request.Description;
        course.DurationHours = request.DurationHours;
        course.Status = request.Status;
        course.UpdatedAt = DateTime.UtcNow;
        course.UpdatedByAccountId = updatedByAccountId;

        _unitOfWork.CourseRepository.Update(course);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CourseResponse(course.CourseId, course.CourseCode, course.CourseName, course.Description, course.DurationHours, course.Status);
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
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
