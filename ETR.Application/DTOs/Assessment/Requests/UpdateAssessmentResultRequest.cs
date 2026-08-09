using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record UpdateAssessmentResultRequest(
    [Range(0, 100, ErrorMessage = "Score must be between 0 and 100")] decimal Score,
    [MaxLength(1000)] string? Remark
);
