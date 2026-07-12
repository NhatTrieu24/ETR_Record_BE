using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssessmentsController : ControllerBase
{
    private readonly IAssessmentService _assessmentService;

    public AssessmentsController(IAssessmentService assessmentService)
    {
        _assessmentService = assessmentService;
    }

    [HttpPost("record")]
    public async Task<IActionResult> RecordAssessment([FromBody] CreateAssessmentResultRequest request, CancellationToken cancellationToken)
    {
        var response = await _assessmentService.RecordAssessmentScoreAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("signoff")]
    public async Task<IActionResult> SignoffSubject([FromBody] CreateSubjectSignoffRequest request, CancellationToken cancellationToken)
    {
        var response = await _assessmentService.SignoffSubjectResultAsync(request, cancellationToken);
        return Ok(response);
    }
}
