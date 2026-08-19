using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.Evidence.Requests;

/// <summary>The FE uploads the file directly to Cloudinary and only ever hands the backend the
/// resulting URL/metadata here — no file bytes cross this API anymore (see EvidenceService.cs).</summary>
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

    [Required, Url, MaxLength(2000)]
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>Cloudinary public_id — optional, kept for a future "delete from Cloudinary" action.</summary>
    [MaxLength(500)]
    public string? PublicId { get; set; }

    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? MimeType { get; set; }

    public long? FileSize { get; set; }
}
