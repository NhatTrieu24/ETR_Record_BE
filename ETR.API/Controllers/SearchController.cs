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
        var result = classes.Where(c => c.ClassName.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        return Ok(result);
    }

    /// <summary>
    /// [Module/Flow]: Khám phá Hệ thống (System Discovery)
    /// [Core Responsibility]: Tìm kiếm các hồ sơ ETR.
    /// [Target Audience]: All Roles
    /// </summary>
    [HttpGet("etrs")]
    public async Task<IActionResult> SearchEtrs([FromQuery] string query, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
        var roleName = _currentUserService.RoleName;

        var etrs = (await _unitOfWork.ETRCourseRecordRepository.GetAllAsync(cancellationToken)).AsEnumerable();
        var enrollments = (await _unitOfWork.CourseEnrollmentRepository.GetAllAsync(cancellationToken)).ToList();
        var profiles = (await _unitOfWork.UserProfileRepository.GetAllAsync(cancellationToken)).ToList();

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
                if (etr.Status.Contains(query, StringComparison.OrdinalIgnoreCase)) return true;
                if (etr.ETRCourseRecordId.ToString() == query) return true;

                var enrollment = enrollments.FirstOrDefault(e => e.EnrollmentId == etr.EnrollmentId);
                var profile = enrollment == null ? null : profiles.FirstOrDefault(p => p.AccountId == enrollment.AccountId);
                return profile != null && profile.FullName.Contains(query, StringComparison.OrdinalIgnoreCase);
            });
        }

        return Ok(etrs.ToList());
    }
}


