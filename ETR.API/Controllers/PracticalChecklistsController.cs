using ETR.Application.DTOs.PracticalChecklist;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Instructor")]
public class PracticalChecklistsController : ControllerBase
{
    private readonly IPracticalChecklistService _service;
    private readonly ICurrentUserService _currentUserService;

    public PracticalChecklistsController(IPracticalChecklistService service, ICurrentUserService currentUserService)
    {
        _service = service;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _service.GetAllPracticalChecklistsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _service.GetPracticalChecklistByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("course/{courseId}/subject/{subjectId}")]
    public async Task<IActionResult> GetBySubject(int courseId, int subjectId, CancellationToken cancellationToken)
    {
        var result = await _service.GetPracticalChecklistsBySubjectAsync(courseId, subjectId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePracticalChecklistRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var result = await _service.CreatePracticalChecklistAsync(request, accountId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.PracticalChecklistId }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePracticalChecklistRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var result = await _service.UpdatePracticalChecklistAsync(id, request, accountId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        await _service.DeletePracticalChecklistAsync(id, accountId, cancellationToken);
        return NoContent();
    }
}
