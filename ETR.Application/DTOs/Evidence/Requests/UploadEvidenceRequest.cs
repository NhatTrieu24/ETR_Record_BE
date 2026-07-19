using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.Evidence.Requests;

public class UploadEvidenceRequest
{
    [Required]
    public int EvidenceTypeId { get; set; }

    [Required]
    public int AccountId { get; set; }

    [Required]
    public int SubjectResultId { get; set; }

    public int? AttendanceRecordId { get; set; }
    
    public int? AssessmentResultId { get; set; }

    [Required]
    public IFormFile File { get; set; } = null!;
}
