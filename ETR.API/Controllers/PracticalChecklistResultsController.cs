using ETR.Application.DTOs.PracticalChecklistResult;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PracticalChecklistResultsController : ControllerBase
{
    private readonly IPracticalChecklistResultService _service;
    private readonly ICurrentUserService _currentUserService;

    public PracticalChecklistResultsController(IPracticalChecklistResultService service, ICurrentUserService currentUserService)
    {
        _service = service;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _service.GetAllPracticalChecklistResultsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _service.GetPracticalChecklistResultByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePracticalChecklistResultRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var result = await _service.CreatePracticalChecklistResultAsync(request, accountId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.PracticalChecklistResultId }, result);
    }

    [HttpPut("{id}/progress")]
    public async Task<IActionResult> UpdateProgress(int id, [FromBody] UpdatePracticalChecklistResultRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var result = await _service.UpdatePracticalChecklistResultAsync(id, request, accountId, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id}/publish")]
    public async Task<IActionResult> Publish(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var result = await _service.PublishPracticalChecklistResultAsync(id, accountId, cancellationToken);
        return Ok(result);
    }
}
