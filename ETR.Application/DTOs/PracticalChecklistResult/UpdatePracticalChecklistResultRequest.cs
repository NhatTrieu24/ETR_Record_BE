using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.PracticalChecklistResult;

public class UpdatePracticalChecklistResultRequest
{
    [Required]
    public bool IsCompleted { get; set; }

    public string? VerificationComment { get; set; }
}
