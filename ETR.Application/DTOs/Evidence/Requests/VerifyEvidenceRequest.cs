using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.Evidence.Requests;

public class VerifyEvidenceRequest
{
    [Required]
    public string VerificationStatus { get; set; } = string.Empty;

    public string? VerificationComment { get; set; }
}
