using ETR.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETR.API.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("attendance")]
    public async Task<ActionResult> GetAttendanceReport(CancellationToken cancellationToken)
    {
        var records = await _unitOfWork.AttendanceRecordRepository.GetAllAsync(cancellationToken);
        int total = records.Count;
        int present = records.Count(r => r.Status == "Present");
        double rate = total > 0 ? (double)present / total * 100 : 0;

        return Ok(new
        {
            ReportName = "Attendance Summary Report",
            TotalRecords = total,
            PresentRecords = present,
            AttendanceRate = rate
        });
    }

    [HttpGet("assessment")]
    public async Task<ActionResult> GetAssessmentReport(CancellationToken cancellationToken)
    {
        var results = await _unitOfWork.AssessmentResultRepository.GetAllAsync(cancellationToken);
        int total = results.Count;
        double averageScore = total > 0 ? (double)results.Average(r => r.Score) : 0.0;

        return Ok(new
        {
            ReportName = "Assessment Summary Report",
            TotalResults = total,
            AverageScore = averageScore
        });
    }

    [HttpGet("completion")]
    public async Task<ActionResult> GetCompletionReport(CancellationToken cancellationToken)
    {
        var etrs = await _unitOfWork.ETRRecordRepository.GetAllAsync(cancellationToken);
        int total = etrs.Count;
        int completed = etrs.Count(e => e.Status == "Completed");
        double completionRate = total > 0 ? (double)completed / total * 100 : 0;

        return Ok(new
        {
            ReportName = "ETR Completion Report",
            TotalETRs = total,
            CompletedETRs = completed,
            CompletionRate = completionRate
        });
    }

    [HttpGet("training")]
    public async Task<ActionResult> GetTrainingReport(CancellationToken cancellationToken)
    {
        var classes = await _unitOfWork.TrainingClassRepository.GetAllAsync(cancellationToken);
        var courses = await _unitOfWork.CourseRepository.GetAllAsync(cancellationToken);

        return Ok(new
        {
            ReportName = "General Training Activity Report",
            TotalCoursesCount = courses.Count,
            TotalClassesCount = classes.Count
        });
    }
}
