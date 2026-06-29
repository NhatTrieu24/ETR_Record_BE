using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LearnerController : ControllerBase
{
    private readonly ILearnerService _learnerService;

    public LearnerController(ILearnerService learnerService)
    {
        _learnerService = learnerService;
    }

    [HttpPost]
    public async Task<ActionResult<Learner>> Create(
        [FromBody] CreateLearnerRequest request,
        CancellationToken cancellationToken)
    {
        var learner = await _learnerService.CreateLearnerAsync(
            request.LearnerCode,
            request.FullName,
            request.IdentificationNumber,
            request.LearnerTypeId,
            cancellationToken);

        return Created($"/api/learner/{learner.LearnerId}", learner);
    }
}

public record CreateLearnerRequest(
    string LearnerCode,
    string FullName,
    string IdentificationNumber,
    int LearnerTypeId);
