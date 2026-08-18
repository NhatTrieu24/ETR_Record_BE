using System.ComponentModel.DataAnnotations;
using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record UpdateEnrollmentRequest(
    int EnrollmentId,
    int LearnerId,
    int ClassId,
    [Required] EnrollmentStatus Status,
    DateTime EnrolledAt);
