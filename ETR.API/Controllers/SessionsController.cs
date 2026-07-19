using ETR.Application.DTOs.Session;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Session Management
/// [Core Responsibility]: Manages class sessions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ICurrentUserService _currentUserService;

    public SessionsController(ISessionService sessionService, ICurrentUserService currentUserService)
    {
        _sessionService = sessionService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Lấy danh sách tất cả các buổi học (sessions).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllSessions(CancellationToken cancellationToken)
    {
        var sessions = await _sessionService.GetAllSessionsAsync(cancellationToken);
        return Ok(sessions);
    }

    /// <summary>
    /// Lấy thông tin một buổi học theo ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSession(int id, CancellationToken cancellationToken)
    {
        var session = await _sessionService.GetSessionByIdAsync(id, cancellationToken);
        return Ok(session);
    }

    /// <summary>
    /// Tạo một buổi học mới.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var session = await _sessionService.CreateSessionAsync(request, accountId, cancellationToken);
        return CreatedAtAction(nameof(GetSession), new { id = session.SessionId }, session);
    }

    /// <summary>
    /// Cập nhật một buổi học hiện có.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSession(int id, [FromBody] UpdateSessionRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        var session = await _sessionService.UpdateSessionAsync(id, request, accountId, cancellationToken);
        return Ok(session);
    }

    /// <summary>
    /// Xóa một buổi học.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSession(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId ?? throw new UnauthorizedAccessException();
        await _sessionService.DeleteSessionAsync(id, accountId, cancellationToken);
        return NoContent();
    }
}
