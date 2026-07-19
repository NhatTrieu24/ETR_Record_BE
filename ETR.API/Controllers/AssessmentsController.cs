using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Thực thi Đào tạo
/// [Core Responsibility]: Records assessment scores and handles subject signoffs.
/// [Target Audience]: Instructor, Admin
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssessmentsController : ControllerBase
{
    private readonly IAssessmentService _assessmentService;
    private readonly ICurrentUserService _currentUserService;

    public AssessmentsController(IAssessmentService assessmentService, ICurrentUserService currentUserService)
    {
        _assessmentService = assessmentService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _assessmentService.GetAllAssessmentResultsAsync(cancellationToken));
    }

    /// <summary>
    /// [Module/Flow]: Thực thi Đào tạo
    /// [Core Responsibility]: Ghi nhận điểm số cho một bài kiểm tra (assessment) cụ thể.
    /// [Target Audience]: Instructor, Admin
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
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var response = await _assessmentService.RecordAssessmentScoreAsync(request, accountId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// [Module/Flow]: Thực thi Đào tạo
    /// [Core Responsibility]: Ký xác nhận (sign off) kết quả môn học.
    /// [Target Audience]: Instructor, Admin
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
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var response = await _assessmentService.SignoffSubjectResultAsync(request, accountId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// [Module/Flow]: Thực thi Đào tạo
    /// [Core Responsibility]: Lấy danh sách kết quả kiểm tra của một học viên cụ thể trong lớp.
    /// [Target Audience]: Instructor, Admin, Student
    /// </summary>
    /// <param name="classStudentId">The ClassStudent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of assessment results.</returns>
    /// <response code="200">Returns the list of assessment results.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the ClassStudent is not found.</response>
    [HttpGet("student/{classStudentId}")]
    [Authorize] // Allow students to also fetch their own results, validation inside service
    public async Task<IActionResult> GetAssessmentResults(int classStudentId, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var response = await _assessmentService.GetAssessmentResultsByClassStudentAsync(classStudentId, accountId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _assessmentService.GetAssessmentResultByIdAsync(id, cancellationToken));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAssessmentResult(int id, [FromBody] UpdateAssessmentResultRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var response = await _assessmentService.UpdateAssessmentResultAsync(id, request, accountId, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAssessmentResult(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        await _assessmentService.DeleteAssessmentResultAsync(id, accountId, cancellationToken);
        return NoContent();
    }
}


