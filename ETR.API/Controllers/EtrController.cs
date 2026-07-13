using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// Handles the workflow of the Electronic Training Record (ETR).
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
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _etrService.SubmitEtrAsync(id, userId, cancellationToken);
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
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _etrService.VerifyEtrAsync(id, userId, cancellationToken);
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
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _etrService.CompleteEtrAsync(id, userId, cancellationToken);
        return Ok(response);
    }
}