using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record CreateSubjectSignoffRequest(
    int SubjectResultId,
    [MaxLength(1000)] string? Comment);
