using System.ComponentModel.DataAnnotations;

namespace ETR.Application.DTOs;

public record UpdateEnrollmentRequest(
    int EnrollmentId,
    int LearnerId,
    int ClassId,
    [Required, MaxLength(20)] string Status,
    DateTime EnrolledAt);
