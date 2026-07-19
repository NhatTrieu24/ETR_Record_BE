using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.Evidence;

public class CreateEvidenceRequest
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
    public string FileName { get; set; } = string.Empty;

    [Required]
    public string FilePath { get; set; } = string.Empty;

    public string? FileExtension { get; set; }
    public string? MimeType { get; set; }
    
    [Required]
    public long FileSize { get; set; }
}
