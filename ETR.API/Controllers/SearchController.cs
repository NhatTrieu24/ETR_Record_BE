using ETR.Application.DTOs;
using ETR.Application.Interfaces;
using ETR.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public SearchController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("learners")]
    public async Task<ActionResult<IEnumerable<LearnerResponse>>> SearchLearners([FromQuery] string q, CancellationToken cancellationToken)
    {
        var learners = await _unitOfWork.LearnerRepository.GetAllAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(q)) return Ok(learners.Select(MapLearnerToResponse));
        var filtered = learners.Where(l => l.LearnerCode.Contains(q, StringComparison.OrdinalIgnoreCase) || 
                                           l.FullName.Contains(q, StringComparison.OrdinalIgnoreCase));
        return Ok(filtered.Select(MapLearnerToResponse));
    }

    [HttpGet("courses")]
    public async Task<ActionResult<IEnumerable<CourseResponse>>> SearchCourses([FromQuery] string q, CancellationToken cancellationToken)
    {
        var courses = await _unitOfWork.CourseRepository.GetAllAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(q)) return Ok(courses.Select(MapCourseToResponse));
        var filtered = courses.Where(c => c.CourseCode.Contains(q, StringComparison.OrdinalIgnoreCase) || 
                                         c.CourseName.Contains(q, StringComparison.OrdinalIgnoreCase));
        return Ok(filtered.Select(MapCourseToResponse));
    }

    [HttpGet("classes")]
    public async Task<ActionResult<IEnumerable<TrainingClassResponse>>> SearchClasses([FromQuery] string q, CancellationToken cancellationToken)
    {
        var classes = await _unitOfWork.TrainingClassRepository.GetAllAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(q)) return Ok(classes.Select(MapClassToResponse));
        var filtered = classes.Where(tc => tc.ClassCode.Contains(q, StringComparison.OrdinalIgnoreCase) || 
                                           tc.ClassName.Contains(q, StringComparison.OrdinalIgnoreCase));
        return Ok(filtered.Select(MapClassToResponse));
    }

    [HttpGet("etr")]
    public async Task<ActionResult<IEnumerable<EtrRecordResponse>>> SearchEtr([FromQuery] string q, CancellationToken cancellationToken)
    {
        var etrs = await _unitOfWork.ETRRecordRepository.GetAllAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(q)) return Ok(etrs.Select(MapEtrToResponse));
        var filtered = etrs.Where(e => e.Status.Contains(q, StringComparison.OrdinalIgnoreCase));
        return Ok(filtered.Select(MapEtrToResponse));
    }

    [HttpGet("evidence")]
    public async Task<ActionResult<IEnumerable<EvidenceFileResponse>>> SearchEvidence([FromQuery] string q, CancellationToken cancellationToken)
    {
        var evidences = await _unitOfWork.EvidenceFileRepository.GetAllAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(q)) return Ok(evidences.Select(MapFileToResponse));
        var filtered = evidences.Where(e => e.FileName.Contains(q, StringComparison.OrdinalIgnoreCase) || 
                                             e.VerificationStatus.Contains(q, StringComparison.OrdinalIgnoreCase));
        return Ok(filtered.Select(MapFileToResponse));
    }

    private static LearnerResponse MapLearnerToResponse(Learner l)
    {
        return new LearnerResponse(
            l.LearnerId, l.LearnerCode, l.FullName, l.DateOfBirth, l.Gender, l.Phone, l.Email, l.IdentificationNumber, l.Organization, l.Status, l.LearnerTypeId);
    }

    private static CourseResponse MapCourseToResponse(Course c)
    {
        return new CourseResponse(c.CourseId, c.CourseCode, c.CourseName, c.Description, c.DurationHours, c.Status);
    }

    private static TrainingClassResponse MapClassToResponse(TrainingClass c)
    {
        return new TrainingClassResponse(c.ClassId, c.ClassCode, c.ClassName, c.CourseId, c.StartDate, c.EndDate, c.Location, c.Capacity, c.Status);
    }

    private static EtrRecordResponse MapEtrToResponse(ETRRecord e)
    {
        return new EtrRecordResponse(
            e.ETRRecordId, e.EnrollmentId, e.Status, e.IsLocked, e.SubmittedAt, e.VerifiedAt, e.CompletedAt);
    }

    private static EvidenceFileResponse MapFileToResponse(EvidenceFile f)
    {
        return new EvidenceFileResponse(
            f.EvidenceFileId, f.EvidenceTypeId, f.FileName, f.FilePath, f.FileExtension, f.MimeType, f.FileSize, f.VerificationStatus, f.QAComment, f.VerifiedBy, f.VerifiedAt, f.UploadedBy, f.UploadedAt);
    }
}
