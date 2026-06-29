using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrollmentController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    [HttpPost]
    public async Task<ActionResult<CreateEnrollmentResult>> Create(
        [FromBody] CreateEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _enrollmentService.CreateEnrollmentAsync(
            request.LearnerId,
            request.ClassId,
            request.CreatedByUserId,
            cancellationToken);

        return Created($"/api/enrollment/{result.Enrollment.EnrollmentId}", result);
    }
}

public record CreateEnrollmentRequest(int LearnerId, int ClassId, int CreatedByUserId);
