using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.PracticalChecklistResult;

public class CreatePracticalChecklistResultRequest
{
    [Required]
    public int SubjectResultId { get; set; }

    public int? SessionId { get; set; }

    [Required]
    public decimal Score { get; set; }

    public string? VerificationComment { get; set; }
}
