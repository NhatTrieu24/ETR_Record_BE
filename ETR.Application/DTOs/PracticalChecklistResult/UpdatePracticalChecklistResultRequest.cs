using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs.PracticalChecklistResult;

public class UpdatePracticalChecklistResultRequest
{
    public int? SessionId { get; set; }

    [Required]
    public bool IsCompleted { get; set; }

    public string? VerificationComment { get; set; }
}
