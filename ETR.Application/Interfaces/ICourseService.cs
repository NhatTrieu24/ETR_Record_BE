using ETR.Application.DTOs;

namespace ETR.Application.Interfaces;

public interface ICourseService
{
    Task<IEnumerable<CourseResponse>> GetAllCoursesAsync(CancellationToken cancellationToken = default);
    Task<CourseResponse> GetCourseByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CourseResponse> CreateCourseAsync(CreateCourseRequest request, int createdByAccountId, CancellationToken cancellationToken = default);
    Task<CourseResponse> UpdateCourseAsync(int id, UpdateCourseRequest request, int updatedByAccountId, CancellationToken cancellationToken = default);
    Task DeleteCourseAsync(int id, int deletedByAccountId, CancellationToken cancellationToken = default);
}
