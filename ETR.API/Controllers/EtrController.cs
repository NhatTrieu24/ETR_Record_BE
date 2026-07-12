using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

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

    [HttpPost("{id}/submit")]
    [Authorize(Roles = "Instructor, Admin")] // Example of Role-Based Authorization
    public async Task<IActionResult> SubmitEtr(int id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _etrService.SubmitEtrAsync(id, userId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id}/verify")]
    [Authorize(Roles = "Verifier, Admin")]
    public async Task<IActionResult> VerifyEtr(int id, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _etrService.VerifyEtrAsync(id, userId, cancellationToken);
        return Ok(response);
    }

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