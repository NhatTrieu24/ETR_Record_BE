using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: ETR Processing
/// [Core Responsibility]: Handles course enrollment operations.
/// [Target Audience]: Admin
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
    /// [Module/Flow]: ETR Processing
    /// [Core Responsibility]: Retrieves all course enrollments.
    /// [Target Audience]: Admin
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of enrollments.</returns>
    /// <response code="200">Returns the list of enrollments.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not an Admin.</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EnrollmentResponse>>> GetAllEnrollments(CancellationToken cancellationToken)
    {
        var enrollments = await _enrollmentService.GetAllEnrollmentsAsync(cancellationToken);
        return Ok(enrollments);
    }

    /// <summary>
    /// [Module/Flow]: ETR Processing
    /// [Core Responsibility]: Retrieves a specific course enrollment by ID.
    /// [Target Audience]: Admin
    /// </summary>
    /// <param name="id">The Enrollment ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested enrollment details.</returns>
    /// <response code="200">Returns the enrollment details.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not an Admin.</response>
    /// <response code="404">If the enrollment is not found.</response>
    [HttpGet("{id}")]
    public async Task<ActionResult<EnrollmentResponse>> GetEnrollmentById(int id, CancellationToken cancellationToken)
    {
        var enrollment = await _enrollmentService.GetEnrollmentByIdAsync(id, cancellationToken);
        return Ok(enrollment);
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
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var response = await _enrollmentService.CreateEnrollmentAsync(request.AccountId, request.ClassId, accountId, cancellationToken);
        return Ok(response);
    }
}
