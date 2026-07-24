using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record CreateAssessmentResultRequest(
    int AssessmentId,
    int AccountId,
    int SubjectResultId,
    decimal Score,
    [MaxLength(1000)] string? Remark,
    int? SessionId = null,
    int? AuthorizedByAccountId = null);
