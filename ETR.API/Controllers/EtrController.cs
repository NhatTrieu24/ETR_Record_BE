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
[Authorize(Roles = "Admin,QA,Student,Instructor,Audit")] // Secure all endpoints in this controller by default
public class EtrController : ControllerBase
{
    private readonly IEtrService _etrService;
    private readonly IApprovalService _approvalService;
    private readonly ICurrentUserService _currentUserService;

    public EtrController(IEtrService etrService, IApprovalService approvalService, ICurrentUserService currentUserService)
    {
        _etrService = etrService;
        _approvalService = approvalService;
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
    [Authorize(Roles = "Instructor,QA,Admin,Audit,Academic")]
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
    [Authorize(Roles = "Student")]
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
    [Authorize(Roles = "Instructor,QA,Admin,Audit,Academic")]
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
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> SubmitEtr(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var response = await _etrService.SubmitEtrAsync(id, accountId, cancellationToken);

        // Tự động tạo ApprovalRequest cho TrainingManager duyệt — trước đây phải tạo thủ công qua
        // POST /api/Approvals, khiến ETR "kẹt" ở Submitted mà không ai biết cần duyệt.
        await _approvalService.CreateApprovalRequestAsync(id, currentApproverId: null, accountId, cancellationToken);

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
    [Authorize(Roles = "QA,Admin")]
    public async Task<IActionResult> VerifyEtr(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _etrService.VerifyEtrAsync(id, accountId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Trả lại ETR để chỉnh sửa (chuyển từ Submitted về Draft).
    /// </summary>
    [HttpPost("{id}/return")]
    [Authorize(Roles = "QA,Admin")]
    public async Task<IActionResult> ReturnEtr(int id, [FromBody] ReturnEtrRequest? request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _etrService.ReturnEtrAsync(id, accountId, request?.Comment, cancellationToken);
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
    /// <response code="403">If the user is not an Admin or TrainingManager.</response>
    [HttpPost("{id}/complete")]
    [Authorize(Roles = "Admin,TrainingManager")]
    public async Task<IActionResult> CompleteEtr(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        var response = await _etrService.CompleteEtrAsync(id, accountId, cancellationToken);
        return Ok(response);
    }
    /// <summary>
    /// [Module/Flow]: Xử lý ETR
    /// [Core Responsibility]: Mở khoá lại một ETR đã Completed/Locked (Re-open) — ngoại lệ duy nhất
    /// cho nguyên tắc bất biến tuyệt đối, bắt buộc phải nêu lý do và luôn được ghi vào Audit Log.
    /// [Target Audience]: Admin
    /// </summary>
    [HttpPost("{id}/reopen")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReopenEtr(int id, [FromBody] ReturnEtrRequest request, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var response = await _etrService.UnlockEtrAsync(id, accountId, request.Comment, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Xóa mềm (soft delete) một hồ sơ ETR.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteEtr(int id, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        await _etrService.DeleteEtrAsync(id, accountId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// [Module/Flow]: Xử lý ETR
    /// [Core Responsibility]: Lấy lịch sử tất cả hồ sơ ETR của một học viên cụ thể.
    /// [Target Audience]: Admin, Instructor, QA, Audit, Student (nếu là chính họ)
    /// </summary>
    [HttpGet("student/{studentId}/history")]
    [Authorize(Roles = "Admin,Instructor,QA,Audit,Student,Academic")]
    public async Task<ActionResult<IEnumerable<EtrRecordResponse>>> GetStudentEtrHistory(int studentId, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        // If Student, only allow accessing their own history
        if (_currentUserService.RoleName == "Student" && accountId != studentId)
        {
            return Forbid();
        }

        var history = await _etrService.GetStudentEtrHistoryAsync(studentId, cancellationToken);
        return Ok(history);
    }

    /// <summary>
    /// [Module/Flow]: Xử lý ETR
    /// [Core Responsibility]: Lấy trạng thái hiện tại (hiệu lực) của các chứng chỉ/ETR của một học viên cụ thể.
    /// [Target Audience]: Admin, Instructor, QA, Audit, Student (nếu là chính họ)
    /// </summary>
    [HttpGet("student/{studentId}/current-status")]
    [Authorize(Roles = "Admin,Instructor,QA,Audit,Student,Academic")]
    public async Task<ActionResult<IEnumerable<StudentEtrStatusResponse>>> GetStudentEtrCurrentStatus(int studentId, CancellationToken cancellationToken)
    {
        var accountId = _currentUserService.AccountId 
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
            
        // If Student, only allow accessing their own status
        if (_currentUserService.RoleName == "Student" && accountId != studentId)
        {
            return Forbid();
        }

        var statusList = await _etrService.GetStudentEtrCurrentStatusAsync(studentId, cancellationToken);
        return Ok(statusList);
    }

    /// <summary>
    /// [Module/Flow]: Xử lý ETR
    /// [Core Responsibility]: Lấy danh sách học viên có ETR sắp hết hạn hoặc đã hết hạn cho một khóa học.
    /// [Target Audience]: Admin, Instructor, QA, TrainingManager
    /// </summary>
    [HttpGet("expiring-students")]
    [Authorize(Roles = "Admin,Instructor,QA,TrainingManager,Academic")]
    public async Task<ActionResult<IEnumerable<ExpiringStudentResponse>>> GetExpiringStudents([FromQuery] int courseId, [FromQuery] int daysThreshold = 30, CancellationToken cancellationToken = default)
    {
        var expiringStudents = await _etrService.GetExpiringStudentsAsync(courseId, daysThreshold, cancellationToken);
        return Ok(expiringStudents);
    }
}
