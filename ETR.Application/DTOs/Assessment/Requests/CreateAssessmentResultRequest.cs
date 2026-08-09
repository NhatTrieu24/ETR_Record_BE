using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record CreateAssessmentResultRequest(
    int AssessmentId,
    int AccountId,
    int SubjectResultId,
    [Range(0, 100, ErrorMessage = "Score must be between 0 and 100")] decimal Score,
    [MaxLength(1000)] string? Remark,
    int? SessionId = null,
    int? AuthorizedByAccountId = null);
