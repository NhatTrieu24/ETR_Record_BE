using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: ETR Processing
/// [Core Responsibility]: Handles the workflow and state transitions of the Electronic Training Record (ETR).
/// [Target Audience]: Instructor, Verifier, Admin
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Secure all endpoints in this controller by default
public class EtrController : ControllerBase
{
    private readonly IEtrService _etrService;
    private readonly ICurrentUserService _currentUserService;

    public EtrController(IEtrService etrService, ICurrentUserService currentUserService)
    {
        _etrService = etrService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// [Module/Flow]: ETR Processing
    /// [Core Responsibility]: Retrieves all ETR records.
    /// [Target Audience]: Instructor, Verifier, Admin
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of ETR records.</returns>
    /// <response code="200">Returns the list of ETR records.</response>
    /// <response code="401">If the user is not authenticated.</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EtrRecordResponse>>> GetAllEtrs(CancellationToken cancellationToken)
    {
        var etrs = await _etrService.GetAllEtrsAsync(cancellationToken);
        return Ok(etrs);
    }

    /// <summary>
    /// [Module/Flow]: ETR Processing
    /// [Core Responsibility]: Retrieves a specific ETR record by ID, including its Subject Results.
    /// [Target Audience]: Instructor, Verifier, Admin
    /// </summary>
    /// <param name="id">The ETR Course Record ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ETR details and subject results.</returns>
    /// <response code="200">Returns the ETR details.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the ETR record is not found.</response>
    [HttpGet("{id}")]
    public async Task<ActionResult<EtrDetailsResponse>> GetEtrById(int id, CancellationToken cancellationToken)
    {
        var etr = await _etrService.GetEtrByIdAsync(id, cancellationToken);
        return Ok(etr);
    }

    /// <summary>
    /// Submits an ETR for verification.
    /// </summary>
    /// <param name="id">The ETR Course Record ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated ETR record.</returns>
    /// <response code="200">Returns the updated record.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not an Instructor or Admin.</response>
    [HttpPost("{id}/submit")]
    [Authorize(Roles = "Instructor, Admin")]
    public async Task<IActionResult> SubmitEtr(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _etrService.SubmitEtrAsync(id, accountId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Verifies a submitted ETR.
    /// </summary>
    /// <param name="id">The ETR Course Record ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated ETR record.</returns>
    /// <response code="200">Returns the updated record.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not a Verifier or Admin.</response>
    [HttpPost("{id}/verify")]
    [Authorize(Roles = "Verifier, Admin")]
    public async Task<IActionResult> VerifyEtr(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _etrService.VerifyEtrAsync(id, accountId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Completes a verified ETR if all conditions are met.
    /// </summary>
    /// <param name="id">The ETR Course Record ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated ETR record.</returns>
    /// <response code="200">Returns the updated record.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not an Admin.</response>
    [HttpPost("{id}/complete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CompleteEtr(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _etrService.CompleteEtrAsync(id, accountId, cancellationToken);
        return Ok(response);
    }
}