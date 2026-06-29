using Microsoft.AspNetCore.Http;

namespace ETR.Application.DTOs;

public class UploadEvidenceRequest
{
    public IFormFile File { get; set; } = null!;
    public int EvidenceTypeId { get; set; }
    public int UploadedBy { get; set; }
    public int LearnerId { get; set; }
    public int ETRRecordId { get; set; }
    public int? AttendanceRecordId { get; set; }
    public int? AssessmentResultId { get; set; }
}
