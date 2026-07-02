using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api")]
public class ClassesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ClassesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    #region Class Management

    [HttpGet("classes")]
    public async Task<ActionResult<IEnumerable<TrainingClassResponse>>> GetClasses(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.TrainingClassRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapClassToResponse));
    }

    [HttpGet("classes/{id:int}")]
    public async Task<ActionResult<TrainingClassResponse>> GetClassById(int id, CancellationToken cancellationToken)
    {
        var tc = await _unitOfWork.TrainingClassRepository.GetByIdAsync(id, cancellationToken);
        if (tc == null) return NotFound($"Không tìm thấy lớp học với ID {id}.");
        return Ok(MapClassToResponse(tc));
    }

    [HttpPost("classes")]
    public async Task<ActionResult<TrainingClassResponse>> CreateClass([FromBody] CreateClassRequest request, CancellationToken cancellationToken)
    {
        var tc = new TrainingClass
        {
            ClassCode = request.ClassCode,
            ClassName = request.ClassName,
            CourseId = request.CourseId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Location = request.Location,
            Capacity = request.Capacity,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.TrainingClassRepository.AddAsync(tc, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return CreatedAtAction(nameof(GetClassById), new { id = tc.ClassId }, MapClassToResponse(tc));
    }

    [HttpPut("classes/{id:int}")]
    public async Task<ActionResult> UpdateClass(int id, [FromBody] UpdateClassRequest request, CancellationToken cancellationToken)
    {
        if (id != request.ClassId) return BadRequest("ID không khớp.");

        var existing = await _unitOfWork.TrainingClassRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy lớp học với ID {id}.");

        existing.ClassCode = request.ClassCode;
        existing.ClassName = request.ClassName;
        existing.StartDate = request.StartDate;
        existing.EndDate = request.EndDate;
        existing.Location = request.Location;
        existing.Capacity = request.Capacity;
        existing.Status = request.Status;
        existing.CourseId = request.CourseId;
        existing.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.TrainingClassRepository.Update(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("classes/{id:int}")]
    public async Task<ActionResult> DeleteClass(int id, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.TrainingClassRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy lớp học với ID {id}.");

        _unitOfWork.TrainingClassRepository.Delete(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("classes/search")]
    public async Task<ActionResult<IEnumerable<TrainingClassResponse>>> SearchClasses([FromQuery] string query, CancellationToken cancellationToken)
    {
        var classes = await _unitOfWork.TrainingClassRepository.GetAllAsync(cancellationToken);
        var filtered = classes.Where(tc => tc.ClassCode.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                                           tc.ClassName.Contains(query, StringComparison.OrdinalIgnoreCase));
        return Ok(filtered.Select(MapClassToResponse));
    }

    [HttpGet("classes/{id:int}/learners")]
    public async Task<ActionResult<IEnumerable<LearnerResponse>>> GetClassLearners(int id, CancellationToken cancellationToken)
    {
        var enrollments = (await _unitOfWork.EnrollmentRepository.GetAllAsync(cancellationToken))
            .Where(e => e.ClassId == id)
            .Select(e => e.LearnerId)
            .ToList();

        var learners = (await _unitOfWork.LearnerRepository.GetAllAsync(cancellationToken))
            .Where(l => enrollments.Contains(l.LearnerId));

        return Ok(learners.Select(l => new LearnerResponse(
            l.LearnerId, l.LearnerCode, l.FullName, l.DateOfBirth, l.Gender, l.Phone, l.Email, l.IdentificationNumber, l.Organization, l.Status, l.LearnerTypeId)));
    }

    [HttpGet("classes/{id:int}/attendance")]
    public async Task<ActionResult<IEnumerable<AttendanceRecordResponse>>> GetClassAttendance(int id, CancellationToken cancellationToken)
    {
        var sessions = (await _unitOfWork.AttendanceSessionRepository.GetAllAsync(cancellationToken))
            .Where(s => s.ClassId == id)
            .Select(s => s.AttendanceSessionId)
            .ToList();

        var records = (await _unitOfWork.AttendanceRecordRepository.GetAllAsync(cancellationToken))
            .Where(r => sessions.Contains(r.AttendanceSessionId));

        return Ok(records.Select(r => new AttendanceRecordResponse(
            r.AttendanceRecordId, r.AttendanceSessionId, r.LearnerId, r.ETRRecordId, r.Status, r.Remarks, r.RecordedBy, r.RecordedAt)));
    }

    [HttpGet("classes/{id:int}/assessments")]
    public async Task<ActionResult<IEnumerable<AssessmentResultResponse>>> GetClassAssessments(int id, CancellationToken cancellationToken)
    {
        var learnerIds = (await _unitOfWork.EnrollmentRepository.GetAllAsync(cancellationToken))
            .Where(e => e.ClassId == id)
            .Select(e => e.LearnerId)
            .ToList();

        var results = (await _unitOfWork.AssessmentResultRepository.GetAllAsync(cancellationToken))
            .Where(r => learnerIds.Contains(r.LearnerId));

        return Ok(results.Select(r => new AssessmentResultResponse(
            r.AssessmentResultId, r.AssessmentComponentId, r.LearnerId, r.ETRRecordId, r.Score, r.ResultStatus, r.InstructorComment, r.RecordedBy, r.RecordedAt, r.PublishedAt, r.IsPublished)));
    }

    #endregion

    #region Instructor Assignment (ClassInstructor)

    [HttpGet("class-instructors")]
    public async Task<ActionResult<IEnumerable<ClassInstructorResponse>>> GetClassInstructors(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.ClassInstructorRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapInstructorToResponse));
    }

    [HttpGet("class-instructors/{id:int}")]
    public async Task<ActionResult<ClassInstructorResponse>> GetClassInstructorById(int id, CancellationToken cancellationToken)
    {
        var ci = await _unitOfWork.ClassInstructorRepository.GetByIdAsync(id, cancellationToken);
        if (ci == null) return NotFound($"Không tìm thấy bản ghi phân công với ID {id}.");
        return Ok(MapInstructorToResponse(ci));
    }

    [HttpPost("class-instructors")]
    public async Task<ActionResult<ClassInstructorResponse>> AssignInstructor([FromBody] AssignInstructorRequest request, CancellationToken cancellationToken)
    {
        var ci = new ClassInstructor
        {
            ClassId = request.ClassId,
            UserId = request.UserId,
            IsPrimaryInstructor = request.IsPrimaryInstructor,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false,
            AssignedAt = DateTime.UtcNow
        };

        await _unitOfWork.ClassInstructorRepository.AddAsync(ci, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return CreatedAtAction(nameof(GetClassInstructorById), new { id = ci.ClassInstructorId }, MapInstructorToResponse(ci));
    }

    [HttpDelete("class-instructors/{id:int}")]
    public async Task<ActionResult> RemoveInstructorAssignment(int id, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.ClassInstructorRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy bản ghi phân công với ID {id}.");

        _unitOfWork.ClassInstructorRepository.Delete(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    #endregion

    private static TrainingClassResponse MapClassToResponse(TrainingClass c)
    {
        return new TrainingClassResponse(c.ClassId, c.ClassCode, c.ClassName, c.CourseId, c.StartDate, c.EndDate, c.Location, c.Capacity, c.Status);
    }

    private static ClassInstructorResponse MapInstructorToResponse(ClassInstructor ci)
    {
        return new ClassInstructorResponse(ci.ClassInstructorId, ci.ClassId, ci.UserId, ci.IsPrimaryInstructor, ci.AssignedAt);
    }
}
