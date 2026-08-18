using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Khám phá Hệ thống (System Discovery)
/// [Core Responsibility]: Provides global search capabilities across classes and ETR records.
/// [Target Audience]: All Roles
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SearchController(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// [Module/Flow]: Khám phá Hệ thống (System Discovery)
    /// [Core Responsibility]: Tìm kiếm các lớp học theo tên.
    /// [Target Audience]: All Roles
    /// </summary>
    [HttpGet("classes")]
    public async Task<IActionResult> SearchClasses([FromQuery] string query, CancellationToken cancellationToken)
    {
        var classes = await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken);
        var courses = (await _unitOfWork.CourseRepository.GetAllAsync(cancellationToken)).ToList();

        var matches = classes.Where(c => string.IsNullOrWhiteSpace(query) || c.ClassName.Contains(query, StringComparison.OrdinalIgnoreCase));

        var result = matches.Select(c =>
        {
            var course = courses.FirstOrDefault(co => co.CourseId == c.CourseId);
            return new ClassSearchResultResponse(
                c.ClassId,
                c.ClassCode,
                c.ClassName,
                course?.CourseCode ?? "-",
                course?.CourseName ?? "-",
                c.Status);
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// [Module/Flow]: Khám phá Hệ thống (System Discovery)
    /// [Core Responsibility]: Tìm kiếm các hồ sơ ETR, có thể lọc thêm theo Khoá học/Giảng viên/Khoảng ngày.
    /// [Target Audience]: All Roles
    /// </summary>
    /// <param name="query">Từ khoá tìm theo Status/ID/tên học viên.</param>
    /// <param name="courseId">Lọc theo khoá học cụ thể.</param>
    /// <param name="instructorId">Lọc theo giảng viên phụ trách lớp.</param>
    /// <param name="dateFrom">Lọc ETR có ngày cấp (hoặc ngày tạo, nếu chưa cấp) từ ngày này trở đi.</param>
    /// <param name="dateTo">Lọc ETR có ngày cấp (hoặc ngày tạo, nếu chưa cấp) tới ngày này.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("etrs")]
    public async Task<IActionResult> SearchEtrs(
        [FromQuery] string? query,
        [FromQuery] int? courseId,
        [FromQuery] int? instructorId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
        var roleName = _currentUserService.RoleName;

        var etrs = (await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken)).AsEnumerable();
        var enrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken)).ToList();
        var profiles = (await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken)).ToList();
        var classes = (await _unitOfWork.ClassRepository.GetAllAsync(cancellationToken)).ToList();
        var courses = (await _unitOfWork.CourseRepository.GetAllAsync(cancellationToken)).ToList();

        // Zero-Trust: Students only search within their own ETRs.
        if (roleName == "Student")
        {
            var myEnrollmentIds = enrollments.Where(e => e.AccountId == accountId).Select(e => e.EnrollmentId).ToHashSet();
            etrs = etrs.Where(e => myEnrollmentIds.Contains(e.EnrollmentId));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            etrs = etrs.Where(etr =>
            {
                if (etr.Status.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)) return true;
                if (etr.ETRCourseRecordId.ToString() == query) return true;

                var enrollment = enrollments.FirstOrDefault(e => e.EnrollmentId == etr.EnrollmentId);
                var profile = enrollment == null ? null : profiles.FirstOrDefault(p => p.AccountId == enrollment.AccountId);
                return profile != null && profile.FullName.Contains(query, StringComparison.OrdinalIgnoreCase);
            });
        }

        if (courseId.HasValue)
        {
            var courseClassIds = classes.Where(c => c.CourseId == courseId.Value).Select(c => c.ClassId).ToHashSet();
            var courseEnrollmentIds = enrollments.Where(e => courseClassIds.Contains(e.ClassId)).Select(e => e.EnrollmentId).ToHashSet();
            etrs = etrs.Where(e => courseEnrollmentIds.Contains(e.EnrollmentId));
        }

        if (instructorId.HasValue)
        {
            var instructorClassIds = _unitOfWork.ClassSubjectRepository.GetQueryable()
                .Where(cs => cs.InstructorAccountId == instructorId.Value)
                .Select(cs => cs.ClassId)
                .ToHashSet();
            var instructorEnrollmentIds = enrollments.Where(e => instructorClassIds.Contains(e.ClassId)).Select(e => e.EnrollmentId).ToHashSet();
            etrs = etrs.Where(e => instructorEnrollmentIds.Contains(e.EnrollmentId));
        }

        if (dateFrom.HasValue)
        {
            etrs = etrs.Where(e => (e.IssuedDate ?? e.CreatedAt) >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            etrs = etrs.Where(e => (e.IssuedDate ?? e.CreatedAt) <= dateTo.Value);
        }

        // Enrich with display names — FE needs student/class/course names, not just raw IDs.
        var result = etrs.Select(etr =>
        {
            var enrollment = enrollments.FirstOrDefault(e => e.EnrollmentId == etr.EnrollmentId);
            var profile = enrollment == null ? null : profiles.FirstOrDefault(p => p.AccountId == enrollment.AccountId);
            var trainingClass = enrollment == null ? null : classes.FirstOrDefault(c => c.ClassId == enrollment.ClassId);
            var course = trainingClass == null ? null : courses.FirstOrDefault(co => co.CourseId == trainingClass.CourseId);

            return new EtrSearchResultResponse(
                etr.ETRCourseRecordId,
                etr.Status,
                profile?.FullName ?? "-",
                trainingClass?.ClassCode ?? "-",
                trainingClass?.ClassName ?? "-",
                course?.CourseCode ?? "-",
                course?.CourseName ?? "-");
        }).ToList();

        return Ok(result);
    }
}


