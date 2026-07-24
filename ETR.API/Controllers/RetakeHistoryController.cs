using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,QA,Instructor,TrainingManager,Academic")]
public class RetakeHistoryController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public RetakeHistoryController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<RetakeHistoryResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var list = (await _unitOfWork.RetakeHistoryRepository.GetAllAsync(cancellationToken))
            .OrderByDescending(r => r.RetakeHistoryId)
            .ToList();
        var pageItems = list.Skip((page - 1) * pageSize).Take(pageSize).Select(MapToResponse);
        return Ok(new PagedResponse<RetakeHistoryResponse>(pageItems, list.Count, page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RetakeHistoryResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _unitOfWork.RetakeHistoryRepository.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound($"RetakeHistory with ID {id} not found.");
        return Ok(MapToResponse(item));
    }

    [HttpGet("by-subject/{subjectResultId:int}")]
    public async Task<ActionResult<IEnumerable<RetakeHistoryResponse>>> GetBySubjectResult(int subjectResultId, CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.RetakeHistoryRepository.GetAllAsync(cancellationToken);
        var filtered = list.Where(r => r.SubjectResultId == subjectResultId);
        return Ok(filtered.Select(MapToResponse));
    }

    private static RetakeHistoryResponse MapToResponse(RetakeHistory r)
    {
        return new RetakeHistoryResponse(
            r.RetakeHistoryId,
            r.SubjectResultId,
            r.RetakeDate,
            r.Reason,
            r.PreviousScore,
            r.NewScore,
            r.AuthorizedByAccountId,
            r.AttemptNo
        );
    }
}

public record RetakeHistoryResponse(
    int RetakeHistoryId,
    int SubjectResultId,
    DateTime RetakeDate,
    string Reason,
    decimal PreviousScore,
    decimal NewScore,
    int AuthorizedByAccountId,
    int AttemptNo
);
