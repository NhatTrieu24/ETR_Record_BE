using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LearnersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public LearnersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLearner(int id, CancellationToken cancellationToken)
    {
        var learner = await _unitOfWork.LearnerRepository.GetByIdAsync(id, cancellationToken);
        if (learner == null) return NotFound();
        return Ok(learner);
    }
}
