using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// Handles assessment recording and subject signoffs.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Instructor, Admin")]
public class AssessmentsController : ControllerBase
{
    private readonly IAssessmentService _assessmentService;
    private readonly ICurrentUserService _currentUserService;

    public AssessmentsController(IAssessmentService assessmentService, ICurrentUserService currentUserService)
    {
        _assessmentService = assessmentService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Records a score for a specific assessment.
    /// </summary>
    /// <param name="request">The assessment result details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recorded assessment result.</returns>
    /// <response code="200">Returns the recorded assessment result.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not an Instructor or Admin.</response>
    [HttpPost("record")]
    public async Task<IActionResult> RecordAssessment([FromBody] CreateAssessmentResultRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var response = await _assessmentService.RecordAssessmentScoreAsync(request, userId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Signs off a subject result.
    /// </summary>
    /// <param name="request">The signoff details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The signoff response.</returns>
    /// <response code="200">Returns the signoff response.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not an Instructor or Admin.</response>
    [HttpPost("signoff")]
    public async Task<IActionResult> SignoffSubject([FromBody] CreateSubjectSignoffRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var response = await _assessmentService.SignoffSubjectResultAsync(request, userId, cancellationToken);
        return Ok(response);
    }
}
