using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api")]
public class ApprovalsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ApprovalsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("approval-requests")]
    public async Task<ActionResult<IEnumerable<ApprovalRequestResponse>>> GetApprovalRequests(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.ApprovalRequestRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapReqToResponse));
    }

    [HttpGet("approval-requests/{id:int}")]
    public async Task<ActionResult<ApprovalRequestResponse>> GetApprovalRequestById(int id, CancellationToken cancellationToken)
    {
        var req = await _unitOfWork.ApprovalRequestRepository.GetByIdAsync(id, cancellationToken);
        if (req == null) return NotFound($"Không tìm thấy yêu cầu phê duyệt với ID {id}.");
        return Ok(MapReqToResponse(req));
    }

    [HttpPost("approval-requests")]
    public async Task<ActionResult<ApprovalRequestResponse>> CreateApprovalRequest([FromBody] CreateApprovalRequest request, CancellationToken cancellationToken)
    {
        var req = new ApprovalRequest
        {
            ETRRecordId = request.ETRRecordId,
            SubmittedBy = request.SubmittedBy,
            CurrentApproverId = request.CurrentApproverId,
            CreatedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow,
            IsDeleted = false,
            CurrentStatus = "Pending"
        };

        await _unitOfWork.ApprovalRequestRepository.AddAsync(req, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return CreatedAtAction(nameof(GetApprovalRequestById), new { id = req.ApprovalRequestId }, MapReqToResponse(req));
    }

    [HttpPost("approval-requests/{id:int}/verify")]
    public async Task<ActionResult<ApprovalRequestResponse>> Verify(int id, [FromBody] ApprovalActionRequest request, CancellationToken cancellationToken)
    {
        return await ProcessApprovalAction(id, "Verify", "Verified", request, cancellationToken);
    }

    [HttpPost("approval-requests/{id:int}/approve")]
    public async Task<ActionResult<ApprovalRequestResponse>> Approve(int id, [FromBody] ApprovalActionRequest request, CancellationToken cancellationToken)
    {
        return await ProcessApprovalAction(id, "Approve", "Approved", request, cancellationToken);
    }

    [HttpPost("approval-requests/{id:int}/reject")]
    public async Task<ActionResult<ApprovalRequestResponse>> Reject(int id, [FromBody] ApprovalActionRequest request, CancellationToken cancellationToken)
    {
        return await ProcessApprovalAction(id, "Reject", "Rejected", request, cancellationToken);
    }

    [HttpPost("approval-requests/{id:int}/return")]
    public async Task<ActionResult<ApprovalRequestResponse>> Return(int id, [FromBody] ApprovalActionRequest request, CancellationToken cancellationToken)
    {
        return await ProcessApprovalAction(id, "Return", "ReturnedForCorrection", request, cancellationToken);
    }

    [HttpGet("approval-history")]
    public async Task<ActionResult<IEnumerable<ApprovalHistoryResponse>>> GetApprovalHistory(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.ApprovalHistoryRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapHistoryToResponse));
    }

    private async Task<ActionResult<ApprovalRequestResponse>> ProcessApprovalAction(
        int id,
        string actionType,
        string targetStatus,
        ApprovalActionRequest request,
        CancellationToken cancellationToken)
    {
        var req = await _unitOfWork.ApprovalRequestRepository.GetByIdAsync(id, cancellationToken);
        if (req == null) return NotFound($"Không tìm thấy yêu cầu phê duyệt với ID {id}.");

        var prevStatus = req.CurrentStatus;
        req.CurrentStatus = targetStatus;
        req.UpdatedAt = DateTime.UtcNow;
        req.CurrentApproverId = request.UserId;

        if (targetStatus == "Approved" || targetStatus == "Rejected")
        {
            req.CompletedAt = DateTime.UtcNow;
        }

        _unitOfWork.ApprovalRequestRepository.Update(req);

        // Record history
        var history = new ApprovalHistory
        {
            ApprovalRequestId = req.ApprovalRequestId,
            ActionBy = request.UserId,
            ActionType = actionType,
            PreviousStatus = prevStatus,
            NewStatus = targetStatus,
            Comments = request.Comment,
            ActionAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.ApprovalHistoryRepository.AddAsync(history, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return Ok(MapReqToResponse(req));
    }

    private static ApprovalRequestResponse MapReqToResponse(ApprovalRequest r)
    {
        return new ApprovalRequestResponse(
            r.ApprovalRequestId,
            r.ETRRecordId,
            r.CurrentStatus,
            r.SubmittedBy,
            r.SubmittedAt,
            r.CurrentApproverId,
            r.CompletedAt);
    }

    private static ApprovalHistoryResponse MapHistoryToResponse(ApprovalHistory h)
    {
        return new ApprovalHistoryResponse(
            h.ApprovalHistoryId,
            h.ApprovalRequestId,
            h.ActionBy,
            h.ActionType,
            h.PreviousStatus,
            h.NewStatus,
            h.Comments,
            h.ActionAt);
    }
}
