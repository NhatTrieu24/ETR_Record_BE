using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult> GetDashboardOverview(CancellationToken cancellationToken)
    {
        return await GetStatistics(cancellationToken);
    }

    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics(CancellationToken cancellationToken)
    {
        var learners = await _unitOfWork.LearnerRepository.GetAllAsync(cancellationToken);
        var classes = await _unitOfWork.TrainingClassRepository.GetAllAsync(cancellationToken);
        var etrs = await _unitOfWork.ETRRecordRepository.GetAllAsync(cancellationToken);

        return Ok(new
        {
            TotalLearners = learners.Count,
            TotalClasses = classes.Count,
            TotalEtrRecords = etrs.Count,
            CompletedEtrs = etrs.Count(e => e.Status == "Completed"),
            PendingEtrs = etrs.Count(e => e.Status == "Submitted"),
            InProgressEtrs = etrs.Count(e => e.Status == "InProgress")
        });
    }

    [HttpGet("course-summary")]
    public async Task<ActionResult> GetCourseSummary(CancellationToken cancellationToken)
    {
        var courses = await _unitOfWork.CourseRepository.GetAllAsync(cancellationToken);
        var classes = await _unitOfWork.TrainingClassRepository.GetAllAsync(cancellationToken);

        var summary = courses.Select(c => new
        {
            CourseId = c.CourseId,
            CourseCode = c.CourseCode,
            CourseName = c.CourseName,
            ClassCount = classes.Count(cl => cl.CourseId == c.CourseId)
        });

        return Ok(summary);
    }

    [HttpGet("qa-summary")]
    public async Task<ActionResult> GetQaSummary(CancellationToken cancellationToken)
    {
        var etrs = await _unitOfWork.ETRRecordRepository.GetAllAsync(cancellationToken);

        return Ok(new
        {
            TotalSubmitted = etrs.Count(e => e.Status == "Submitted"),
            TotalVerified = etrs.Count(e => e.Status == "Verified"),
            TotalCompleted = etrs.Count(e => e.Status == "Completed")
        });
    }

    [HttpGet("completion-rate")]
    public async Task<ActionResult> GetCompletionRate(CancellationToken cancellationToken)
    {
        var etrs = await _unitOfWork.ETRRecordRepository.GetAllAsync(cancellationToken);
        int total = etrs.Count;
        int completed = etrs.Count(e => e.Status == "Completed");
        double rate = total > 0 ? (double)completed / total * 100 : 0;

        return Ok(new
        {
            TotalETRs = total,
            CompletedETRs = completed,
            CompletionRate = rate
        });
    }
}
