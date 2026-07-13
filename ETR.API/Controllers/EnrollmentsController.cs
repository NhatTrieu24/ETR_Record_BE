using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// Handles enrollment operations for Learners.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly ICurrentUserService _currentUserService;

    public EnrollmentsController(IEnrollmentService enrollmentService, ICurrentUserService currentUserService)
    {
        _enrollmentService = enrollmentService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Creates a new course enrollment for a learner.
    /// </summary>
    /// <param name="request">The enrollment details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created enrollment response.</returns>
    /// <response code="200">Returns the newly created enrollment.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not an Admin.</response>
    [HttpPost]
    public async Task<IActionResult> CreateEnrollment([FromBody] CreateEnrollmentRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var response = await _enrollmentService.CreateEnrollmentAsync(request.LearnerId, request.ClassId, userId, cancellationToken);
        return Ok(response);
    }
}
