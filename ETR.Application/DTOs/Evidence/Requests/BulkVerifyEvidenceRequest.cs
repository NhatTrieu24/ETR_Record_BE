using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.Evidence.Requests;

public class BulkVerifyEvidenceRequest
{
    [Required]
    [MinLength(1)]
    public List<int> EvidenceIds { get; set; } = new();

    [Required]
    public string VerificationStatus { get; set; } = string.Empty;

    public string? VerificationComment { get; set; }
}
