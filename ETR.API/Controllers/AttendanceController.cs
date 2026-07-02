using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api")]
public class AttendanceController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AttendanceController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    #region Attendance Session

    [HttpGet("attendance-sessions")]
    public async Task<ActionResult<IEnumerable<AttendanceSessionResponse>>> GetSessions(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.AttendanceSessionRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapSessionToResponse));
    }

    [HttpGet("attendance-sessions/{id:int}")]
    public async Task<ActionResult<AttendanceSessionResponse>> GetSessionById(int id, CancellationToken cancellationToken)
    {
        var session = await _unitOfWork.AttendanceSessionRepository.GetByIdAsync(id, cancellationToken);
        if (session == null) return NotFound($"Không tìm thấy phiên điểm danh với ID {id}.");
        return Ok(MapSessionToResponse(session));
    }

    [HttpPost("attendance-sessions")]
    public async Task<ActionResult<AttendanceSessionResponse>> CreateSession([FromBody] CreateAttendanceSessionRequest request, CancellationToken cancellationToken)
    {
        var session = new AttendanceSession
        {
            ClassId = request.ClassId,
            SessionTitle = request.SessionTitle,
            SessionDate = request.SessionDate,
            Location = request.Location,
            IsConfirmed = false,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.AttendanceSessionRepository.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return CreatedAtAction(nameof(GetSessionById), new { id = session.AttendanceSessionId }, MapSessionToResponse(session));
    }

    [HttpPut("attendance-sessions/{id:int}")]
    public async Task<ActionResult> UpdateSession(int id, [FromBody] UpdateAttendanceSessionRequest request, CancellationToken cancellationToken)
    {
        if (id != request.AttendanceSessionId) return BadRequest("ID không khớp.");

        var existing = await _unitOfWork.AttendanceSessionRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy phiên điểm danh với ID {id}.");

        existing.ClassId = request.ClassId;
        existing.SessionDate = request.SessionDate;
        existing.SessionTitle = request.SessionTitle;
        existing.Location = request.Location;
        existing.IsConfirmed = request.IsConfirmed;
        existing.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.AttendanceSessionRepository.Update(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("attendance-sessions/{id:int}")]
    public async Task<ActionResult> DeleteSession(int id, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.AttendanceSessionRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy phiên điểm danh với ID {id}.");

        _unitOfWork.AttendanceSessionRepository.Delete(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("attendance-sessions/{id:int}/confirm")]
    public async Task<ActionResult<AttendanceSessionResponse>> ConfirmSession(int id, [FromBody] ConfirmSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await _unitOfWork.AttendanceSessionRepository.GetByIdAsync(id, cancellationToken);
        if (session == null) return NotFound($"Không tìm thấy phiên điểm danh với ID {id}.");

        session.ConfirmedBy = request.UserId;
        session.ConfirmedAt = DateTime.UtcNow;
        session.IsConfirmed = true;
        session.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.AttendanceSessionRepository.Update(session);
        await _unitOfWork.SaveAsync(cancellationToken);

        return Ok(MapSessionToResponse(session));
    }

    #endregion

    #region Attendance Record

    [HttpGet("attendance-records")]
    public async Task<ActionResult<IEnumerable<AttendanceRecordResponse>>> GetRecords(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.AttendanceRecordRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapRecordToResponse));
    }

    [HttpGet("attendance-records/{id:int}")]
    public async Task<ActionResult<AttendanceRecordResponse>> GetRecordById(int id, CancellationToken cancellationToken)
    {
        var record = await _unitOfWork.AttendanceRecordRepository.GetByIdAsync(id, cancellationToken);
        if (record == null) return NotFound($"Không tìm thấy bản ghi điểm danh với ID {id}.");
        return Ok(MapRecordToResponse(record));
    }

    [HttpPost("attendance-records")]
    public async Task<ActionResult<AttendanceRecordResponse>> CreateRecord([FromBody] CreateAttendanceRecordRequest request, CancellationToken cancellationToken)
    {
        var record = new AttendanceRecord
        {
            AttendanceSessionId = request.AttendanceSessionId,
            LearnerId = request.LearnerId,
            ETRRecordId = request.ETRRecordId,
            Status = request.Status,
            Remarks = request.Remarks,
            RecordedBy = request.RecordedBy,
            RecordedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.AttendanceRecordRepository.AddAsync(record, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return CreatedAtAction(nameof(GetRecordById), new { id = record.AttendanceRecordId }, MapRecordToResponse(record));
    }

    [HttpPut("attendance-records/{id:int}")]
    public async Task<ActionResult> UpdateRecord(int id, [FromBody] UpdateAttendanceRecordRequest request, CancellationToken cancellationToken)
    {
        if (id != request.AttendanceRecordId) return BadRequest("ID không khớp.");

        var existing = await _unitOfWork.AttendanceRecordRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy bản ghi điểm danh với ID {id}.");

        existing.AttendanceSessionId = request.AttendanceSessionId;
        existing.LearnerId = request.LearnerId;
        existing.ETRRecordId = request.ETRRecordId;
        existing.Status = request.Status;
        existing.Remarks = request.Remarks;
        existing.RecordedBy = request.RecordedBy;
        existing.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.AttendanceRecordRepository.Update(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("attendance-records/{id:int}")]
    public async Task<ActionResult> DeleteRecord(int id, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.AttendanceRecordRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy bản ghi điểm danh với ID {id}.");

        _unitOfWork.AttendanceRecordRepository.Delete(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("attendance-records/bulk")]
    public async Task<ActionResult> CreateBulkRecords([FromBody] BulkAttendanceRequest request, CancellationToken cancellationToken)
    {
        foreach (var item in request.Records)
        {
            var record = new AttendanceRecord
            {
                AttendanceSessionId = item.AttendanceSessionId,
                LearnerId = item.LearnerId,
                ETRRecordId = item.EtrRecordId,
                Status = item.Status,
                Remarks = item.Remarks,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                RecordedBy = request.RecordedByUserId,
                RecordedAt = DateTime.UtcNow
            };
            await _unitOfWork.AttendanceRecordRepository.AddAsync(record, cancellationToken);
        }

        await _unitOfWork.SaveAsync(cancellationToken);
        return Ok("Điểm danh danh sách học viên thành công.");
    }

    #endregion

    private static AttendanceSessionResponse MapSessionToResponse(AttendanceSession s)
    {
        return new AttendanceSessionResponse(
            s.AttendanceSessionId,
            s.ClassId,
            s.SessionTitle,
            s.SessionDate,
            s.Location,
            s.IsConfirmed,
            s.ConfirmedBy,
            s.ConfirmedAt);
    }

    private static AttendanceRecordResponse MapRecordToResponse(AttendanceRecord r)
    {
        return new AttendanceRecordResponse(
            r.AttendanceRecordId,
            r.AttendanceSessionId,
            r.LearnerId,
            r.ETRRecordId,
            r.Status,
            r.Remarks,
            r.RecordedBy,
            r.RecordedAt);
    }
}
