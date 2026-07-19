using ETR.Application.DTOs.EvidenceType;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EvidenceTypesController : ControllerBase
{
    private readonly IEvidenceTypeService _evidenceTypeService;
    private readonly ICurrentUserService _currentUserService;

    public EvidenceTypesController(IEvidenceTypeService evidenceTypeService, ICurrentUserService currentUserService)
    {
        _evidenceTypeService = evidenceTypeService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _evidenceTypeService.GetAllEvidenceTypesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _evidenceTypeService.GetEvidenceTypeByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEvidenceTypeRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var result = await _evidenceTypeService.CreateEvidenceTypeAsync(request, accountId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.EvidenceTypeId }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEvidenceTypeRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var result = await _evidenceTypeService.UpdateEvidenceTypeAsync(id, request, accountId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        await _evidenceTypeService.DeleteEvidenceTypeAsync(id, accountId, cancellationToken);
        return NoContent();
    }
}
