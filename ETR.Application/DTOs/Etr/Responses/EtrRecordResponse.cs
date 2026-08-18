using ETR.Domain.Enums;

namespace ETR.Application.DTOs;

public record EtrRecordResponse(
    int ETRCourseRecordId,
    int EnrollmentId,
    EtrStatus Status,
    bool IsLocked,
    DateTime? SubmittedAt,
    DateTime? VerifiedAt,
    DateTime? CompletedAt,
    DateTime? IssuedDate,
    DateTime? ExpiryDate,
    int? PreviousRecordId);
