using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.PracticalChecklistResult;

public class CreatePracticalChecklistResultRequest
{
    [Required]
    public int SubjectResultId { get; set; }

    public int? SessionId { get; set; }

    [Required]
    [Range(0, 100, ErrorMessage = "Score must be between 0 and 100")]
    public decimal Score { get; set; }

    public string? VerificationComment { get; set; }
}
