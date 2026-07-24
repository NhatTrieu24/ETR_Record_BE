using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record UpdateAssessmentResultRequest(
    decimal Score,
    [MaxLength(1000)] string? Remark
);
