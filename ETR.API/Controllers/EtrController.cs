using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

/// <summary>
/// [Module/Flow]: Xử lý ETR
/// [Core Responsibility]: Handles the workflow and state transitions of the Electronic Training Record (ETR).
/// [Target Audience]: Instructor, QA, Admin
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
    /// [Module/Flow]: Xử lý ETR
    /// [Core Responsibility]: Lấy danh sách tất cả các hồ sơ ETR.
    /// [Target Audience]: Instructor, QA, Admin
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
    /// [Module/Flow]: Xử lý ETR
    /// [Core Responsibility]: Lấy danh sách hồ sơ ETR của học viên hiện đang xác thực.
    /// [Target Audience]: Student
    /// </summary>
    [HttpGet("my-etr")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<EtrRecordResponse>>> GetMyEtrs(CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var etrs = await _etrService.GetMyEtrsAsync(accountId, cancellationToken);
        return Ok(etrs);
    }

    /// <summary>
    /// [Module/Flow]: Xử lý ETR
    /// [Core Responsibility]: Lấy thông tin một hồ sơ ETR cụ thể theo ID, bao gồm cả Kết quả Môn học.
    /// [Target Audience]: Instructor, QA, Admin
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
    /// Gửi một ETR để chờ xác minh (verification).
    /// </summary>
    /// <param name="id">The ETR Course Record ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated ETR record.</returns>
    /// <response code="200">Returns the updated record.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not an Instructor or Admin.</response>
    [HttpPost("{id}/submit")]
    [Authorize]
    public async Task<IActionResult> SubmitEtr(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _etrService.SubmitEtrAsync(id, accountId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Xác minh một ETR đã được gửi.
    /// </summary>
    /// <param name="id">The ETR Course Record ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated ETR record.</returns>
    /// <response code="200">Returns the updated record.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not a QA or Admin.</response>
    [HttpPost("{id}/verify")]
    [Authorize]
    public async Task<IActionResult> VerifyEtr(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _etrService.VerifyEtrAsync(id, accountId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Hoàn tất một ETR đã được xác minh nếu đáp ứng đủ mọi điều kiện.
    /// </summary>
    /// <param name="id">The ETR Course Record ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated ETR record.</returns>
    /// <response code="200">Returns the updated record.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="403">If the user is not an Admin.</response>
    [HttpPost("{id}/complete")]
    [Authorize]
    public async Task<IActionResult> CompleteEtr(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _etrService.CompleteEtrAsync(id, accountId, cancellationToken);
        return Ok(response);
    }
}


