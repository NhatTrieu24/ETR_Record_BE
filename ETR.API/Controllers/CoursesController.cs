using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api")]
public class CoursesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public CoursesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    #region Course Management

    [HttpGet("courses")]
    public async Task<ActionResult<IEnumerable<CourseResponse>>> GetCourses(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.CourseRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapCourseToResponse));
    }

    [HttpGet("courses/{id:int}")]
    public async Task<ActionResult<CourseResponse>> GetCourseById(int id, CancellationToken cancellationToken)
    {
        var course = await _unitOfWork.CourseRepository.GetByIdAsync(id, cancellationToken);
        if (course == null) return NotFound($"Không tìm thấy khóa học với ID {id}.");
        return Ok(MapCourseToResponse(course));
    }

    [HttpPost("courses")]
    public async Task<ActionResult<CourseResponse>> CreateCourse([FromBody] CreateCourseRequest request, CancellationToken cancellationToken)
    {
        var course = new Course
        {
            CourseCode = request.CourseCode,
            CourseName = request.CourseName,
            Description = request.Description,
            DurationHours = request.DurationHours,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.CourseRepository.AddAsync(course, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCourseById), new { id = course.CourseId }, MapCourseToResponse(course));
    }

    [HttpPut("courses/{id:int}")]
    public async Task<ActionResult> UpdateCourse(int id, [FromBody] UpdateCourseRequest request, CancellationToken cancellationToken)
    {
        if (id != request.CourseId) return BadRequest("ID không khớp.");

        var existing = await _unitOfWork.CourseRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy khóa học với ID {id}.");

        existing.CourseCode = request.CourseCode;
        existing.CourseName = request.CourseName;
        existing.Description = request.Description;
        existing.DurationHours = request.DurationHours;
        existing.Status = request.Status;
        existing.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.CourseRepository.Update(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("courses/{id:int}")]
    public async Task<ActionResult> DeleteCourse(int id, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.CourseRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy khóa học với ID {id}.");

        _unitOfWork.CourseRepository.Delete(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("courses/{id:int}/requirements")]
    public async Task<ActionResult<IEnumerable<CompletionRequirementResponse>>> GetCourseRequirements(int id, CancellationToken cancellationToken)
    {
        var requirements = await _unitOfWork.CompletionRequirementRepository.GetAllAsync(cancellationToken);
        var filtered = requirements.Where(r => r.CourseId == id);
        return Ok(filtered.Select(MapRequirementToResponse));
    }

    [HttpGet("courses/{id:int}/assessment-components")]
    public async Task<ActionResult<IEnumerable<AssessmentComponentResponse>>> GetCourseAssessmentComponents(int id, CancellationToken cancellationToken)
    {
        var components = await _unitOfWork.AssessmentComponentRepository.GetAllAsync(cancellationToken);
        var filtered = components.Where(c => c.CourseId == id);
        return Ok(filtered.Select(MapComponentToResponse));
    }

    [HttpGet("courses/{id:int}/checklist")]
    public async Task<ActionResult<IEnumerable<ChecklistItemResponse>>> GetCourseChecklist(int id, CancellationToken cancellationToken)
    {
        var templates = await _unitOfWork.ETRChecklistTemplateRepository.GetAllAsync(cancellationToken);
        var activeTemplate = templates.FirstOrDefault(t => t.CourseId == id && t.IsActive);
        if (activeTemplate == null) return NotFound("Không tìm thấy mẫu checklist hoạt động cho khóa học này.");

        var items = await _unitOfWork.ETRChecklistItemRepository.GetAllAsync(cancellationToken);
        var filtered = items.Where(i => i.TemplateId == activeTemplate.TemplateId).OrderBy(i => i.DisplayOrder);
        return Ok(filtered.Select(MapItemToResponse));
    }

    #endregion

    #region Completion Requirements

    [HttpGet("completion-requirements")]
    public async Task<ActionResult<IEnumerable<CompletionRequirementResponse>>> GetCompletionRequirements(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.CompletionRequirementRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapRequirementToResponse));
    }

    [HttpGet("completion-requirements/{id:int}")]
    public async Task<ActionResult<CompletionRequirementResponse>> GetCompletionRequirementById(int id, CancellationToken cancellationToken)
    {
        var req = await _unitOfWork.CompletionRequirementRepository.GetByIdAsync(id, cancellationToken);
        if (req == null) return NotFound($"Không tìm thấy yêu cầu hoàn thành với ID {id}.");
        return Ok(MapRequirementToResponse(req));
    }

    [HttpPost("completion-requirements")]
    public async Task<ActionResult<CompletionRequirementResponse>> CreateCompletionRequirement([FromBody] CreateCompletionRequirementRequest request, CancellationToken cancellationToken)
    {
        var req = new CompletionRequirement
        {
            CourseId = request.CourseId,
            RequirementName = request.RequirementName,
            Description = request.Description,
            IsMandatory = request.IsMandatory,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.CompletionRequirementRepository.AddAsync(req, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return CreatedAtAction(nameof(GetCompletionRequirementById), new { id = req.RequirementId }, MapRequirementToResponse(req));
    }

    [HttpPut("completion-requirements/{id:int}")]
    public async Task<ActionResult> UpdateCompletionRequirement(int id, [FromBody] UpdateCompletionRequirementRequest request, CancellationToken cancellationToken)
    {
        if (id != request.RequirementId) return BadRequest("ID không khớp.");

        var existing = await _unitOfWork.CompletionRequirementRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy yêu cầu hoàn thành với ID {id}.");

        existing.CourseId = request.CourseId;
        existing.RequirementName = request.RequirementName;
        existing.Description = request.Description;
        existing.IsMandatory = request.IsMandatory;
        existing.DisplayOrder = request.DisplayOrder;
        existing.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.CompletionRequirementRepository.Update(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("completion-requirements/{id:int}")]
    public async Task<ActionResult> DeleteCompletionRequirement(int id, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.CompletionRequirementRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy yêu cầu hoàn thành với ID {id}.");

        _unitOfWork.CompletionRequirementRepository.Delete(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    #endregion

    #region Assessment Components

    [HttpGet("assessment-components")]
    public async Task<ActionResult<IEnumerable<AssessmentComponentResponse>>> GetAssessmentComponents(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.AssessmentComponentRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapComponentToResponse));
    }

    [HttpGet("assessment-components/{id:int}")]
    public async Task<ActionResult<AssessmentComponentResponse>> GetAssessmentComponentById(int id, CancellationToken cancellationToken)
    {
        var comp = await _unitOfWork.AssessmentComponentRepository.GetByIdAsync(id, cancellationToken);
        if (comp == null) return NotFound($"Không tìm thấy cấu phần đánh giá với ID {id}.");
        return Ok(MapComponentToResponse(comp));
    }

    [HttpPost("assessment-components")]
    public async Task<ActionResult<AssessmentComponentResponse>> CreateAssessmentComponent([FromBody] CreateAssessmentComponentRequest request, CancellationToken cancellationToken)
    {
        var comp = new AssessmentComponent
        {
            CourseId = request.CourseId,
            ComponentName = request.ComponentName,
            AssessmentType = request.AssessmentType,
            Weight = request.Weight,
            PassingScore = request.PassingScore,
            IsRequired = request.IsRequired,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.AssessmentComponentRepository.AddAsync(comp, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAssessmentComponentById), new { id = comp.AssessmentComponentId }, MapComponentToResponse(comp));
    }

    [HttpPut("assessment-components/{id:int}")]
    public async Task<ActionResult> UpdateAssessmentComponent(int id, [FromBody] UpdateAssessmentComponentRequest request, CancellationToken cancellationToken)
    {
        if (id != request.AssessmentComponentId) return BadRequest("ID không khớp.");

        var existing = await _unitOfWork.AssessmentComponentRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy cấu phần đánh giá với ID {id}.");

        existing.CourseId = request.CourseId;
        existing.ComponentName = request.ComponentName;
        existing.AssessmentType = request.AssessmentType;
        existing.Weight = request.Weight;
        existing.PassingScore = request.PassingScore;
        existing.IsRequired = request.IsRequired;
        existing.DisplayOrder = request.DisplayOrder;
        existing.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.AssessmentComponentRepository.Update(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("assessment-components/{id:int}")]
    public async Task<ActionResult> DeleteAssessmentComponent(int id, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.AssessmentComponentRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy cấu phần đánh giá với ID {id}.");

        _unitOfWork.AssessmentComponentRepository.Delete(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    #endregion

    #region Checklist Items

    [HttpGet("checklist-items")]
    public async Task<ActionResult<IEnumerable<ChecklistItemResponse>>> GetChecklistItems(CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.ETRChecklistItemRepository.GetAllAsync(cancellationToken);
        return Ok(list.Select(MapItemToResponse));
    }

    [HttpGet("checklist-items/{id:int}")]
    public async Task<ActionResult<ChecklistItemResponse>> GetChecklistItemById(int id, CancellationToken cancellationToken)
    {
        var item = await _unitOfWork.ETRChecklistItemRepository.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound($"Không tìm thấy đầu mục checklist với ID {id}.");
        return Ok(MapItemToResponse(item));
    }

    [HttpPost("checklist-items")]
    public async Task<ActionResult<ChecklistItemResponse>> CreateChecklistItem([FromBody] CreateChecklistItemRequest request, CancellationToken cancellationToken)
    {
        var item = new ETRChecklistItem
        {
            TemplateId = request.TemplateId,
            ItemName = request.ItemName,
            Description = request.Description,
            IsRequired = request.IsRequired,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.ETRChecklistItemRepository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return CreatedAtAction(nameof(GetChecklistItemById), new { id = item.ChecklistItemId }, MapItemToResponse(item));
    }

    [HttpPut("checklist-items/{id:int}")]
    public async Task<ActionResult> UpdateChecklistItem(int id, [FromBody] UpdateChecklistItemRequest request, CancellationToken cancellationToken)
    {
        if (id != request.ETRChecklistItemId) return BadRequest("ID không khớp.");

        var existing = await _unitOfWork.ETRChecklistItemRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy đầu mục checklist với ID {id}.");

        existing.TemplateId = request.TemplateId;
        existing.ItemName = request.ItemName;
        existing.Description = request.Description;
        existing.IsRequired = request.IsRequired;
        existing.DisplayOrder = request.DisplayOrder;
        existing.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.ETRChecklistItemRepository.Update(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("checklist-items/{id:int}")]
    public async Task<ActionResult> DeleteChecklistItem(int id, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.ETRChecklistItemRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null) return NotFound($"Không tìm thấy đầu mục checklist với ID {id}.");

        _unitOfWork.ETRChecklistItemRepository.Delete(existing);
        await _unitOfWork.SaveAsync(cancellationToken);

        return NoContent();
    }

    #endregion

    private static CourseResponse MapCourseToResponse(Course c)
    {
        return new CourseResponse(c.CourseId, c.CourseCode, c.CourseName, c.Description, c.DurationHours, c.Status);
    }

    private static CompletionRequirementResponse MapRequirementToResponse(CompletionRequirement r)
    {
        return new CompletionRequirementResponse(r.RequirementId, r.CourseId, r.RequirementName, r.Description, r.IsMandatory, r.DisplayOrder);
    }

    private static AssessmentComponentResponse MapComponentToResponse(AssessmentComponent c)
    {
        return new AssessmentComponentResponse(c.AssessmentComponentId, c.CourseId, c.ComponentName, c.AssessmentType, c.Weight, c.PassingScore, c.IsRequired, c.DisplayOrder);
    }

    private static ChecklistItemResponse MapItemToResponse(ETRChecklistItem i)
    {
        return new ChecklistItemResponse(i.ChecklistItemId, i.TemplateId, i.ItemName, i.Description, i.IsRequired, i.DisplayOrder);
    }
}
