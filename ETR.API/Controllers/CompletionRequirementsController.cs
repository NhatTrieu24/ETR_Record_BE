using ETR.Application.DTOs.CompletionRequirement;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Academic,TrainingManager")]
public class CompletionRequirementsController : ControllerBase
{
    private readonly ICompletionRequirementService _service;
    private readonly ICurrentUserService _currentUserService;

    public CompletionRequirementsController(ICompletionRequirementService service, ICurrentUserService currentUserService)
    {
        _service = service;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _service.GetAllCompletionRequirementsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _service.GetCompletionRequirementByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("course/{courseId}")]
    public async Task<IActionResult> GetByCourse(int courseId, CancellationToken cancellationToken)
    {
        var result = await _service.GetCompletionRequirementsByCourseAsync(courseId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCompletionRequirementRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var result = await _service.CreateCompletionRequirementAsync(request, accountId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.RequirementId }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCompletionRequirementRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var result = await _service.UpdateCompletionRequirementAsync(id, request, accountId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        await _service.DeleteCompletionRequirementAsync(id, accountId, cancellationToken);
        return NoContent();
    }
}
